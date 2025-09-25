#!/usr/bin/env python3
"""
GitHub Project v2 Status Updater

Updates GitHub Project v2 item status based on issue and PR events.
Supports the complete issue lifecycle automation:
- Issue opened → Backlog
- Issue assigned → Ready
- PR created (draft) → In progress
- PR ready for review → In review
- PR merged/issue closed → Done
"""

import argparse
import os
import re
import sys

import requests


class GitHubProjectUpdater:
    def __init__(
        self,
        token: str,
        owner: str,
        repo: str,
        project_owner: str | None = None,
        project_number: int = 3,
    ):
        """Initialize the GitHub Project updater.

        Args:
            token: GitHub Personal Access Token with project and repo scopes
            owner: Repository owner
            repo: Repository name
            project_owner: Project owner (default: same as repo owner)
            project_number: Project number (default: 3)
        """
        self.token = token
        self.owner = owner
        self.repo = repo
        self.project_owner = project_owner or owner
        self.project_number = project_number
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "Content-Type": "application/json",
            "X-GitHub-Api-Version": "2022-11-28",
        }
        self.graphql_url = "https://api.github.com/graphql"
        self.rest_url = "https://api.github.com"

    def execute_graphql(self, query: str, variables: dict | None = None) -> dict:
        """Execute a GraphQL query against GitHub API."""
        payload = {"query": query}
        if variables:
            payload["variables"] = variables

        response = requests.post(self.graphql_url, headers=self.headers, json=payload, timeout=30)

        if response.status_code != 200:
            raise Exception(f"GraphQL request failed: {response.status_code} - {response.text}")

        result = response.json()

        if "errors" in result:
            raise Exception(f"GraphQL errors: {result['errors']}")

        return result["data"]

    def add_issue_to_project(self, issue_number: int) -> str:
        """Add an issue to the project and return the item ID."""
        # Get issue node ID
        issue_query = """
        query($owner: String!, $repo: String!, $number: Int!) {
          repository(owner: $owner, name: $repo) {
            issue(number: $number) {
              id
              title
            }
          }
        }
        """

        issue_response = self.execute_graphql(
            issue_query, {"owner": self.owner, "repo": self.repo, "number": issue_number}
        )

        issue_id = issue_response["repository"]["issue"]["id"]

        # Get project ID
        project_query = """
        query($login: String!, $number: Int!) {
          user(login: $login) {
            projectV2(number: $number) {
              id
              title
            }
          }
        }
        """

        project_response = self.execute_graphql(
            project_query, {"login": self.project_owner, "number": self.project_number}
        )

        project_id = project_response["user"]["projectV2"]["id"]

        # Add issue to project
        add_mutation = """
        mutation($projectId: ID!, $contentId: ID!) {
          addProjectV2ItemById(input: {
            projectId: $projectId
            contentId: $contentId
          }) {
            item {
              id
            }
          }
        }
        """

        add_response = self.execute_graphql(
            add_mutation, {"projectId": project_id, "contentId": issue_id}
        )

        item_id = add_response["addProjectV2ItemById"]["item"]["id"]
        print(f"✅ Added issue #{issue_number} to project (Item ID: {item_id})")
        return item_id

    def get_issue_project_items(self, issue_number: int) -> dict:
        """Get issue details including project items."""
        query = """
        query($owner: String!, $repo: String!, $number: Int!) {
          repository(owner: $owner, name: $repo) {
            issue(number: $number) {
              id
              title
              assignees(first: 10) {
                totalCount
              }
              projectItems(first: 10) {
                nodes {
                  id
                  project {
                    id
                    title
                    number
                  }
                }
              }
            }
          }
        }
        """

        variables = {"owner": self.owner, "repo": self.repo, "number": issue_number}
        data = self.execute_graphql(query, variables)
        return data["repository"]["issue"]

    def get_project_status_field(self, project_id: str) -> dict:
        """Get project Status field details."""
        query = """
        query($projectId: ID!) {
          node(id: $projectId) {
            ... on ProjectV2 {
              fields(first: 20) {
                nodes {
                  ... on ProjectV2SingleSelectField {
                    id
                    name
                    options {
                      id
                      name
                    }
                  }
                }
              }
            }
          }
        }
        """

        variables = {"projectId": project_id}
        data = self.execute_graphql(query, variables)

        # Find Status field
        fields = data["node"]["fields"]["nodes"]
        status_field = next((field for field in fields if field.get("name") == "Status"), None)

        if not status_field:
            raise Exception("No Status field found in project")

        return status_field

    def update_project_item_status(
        self, project_id: str, item_id: str, field_id: str, option_id: str
    ) -> None:
        """Update project item status field."""
        mutation = """
        mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
          updateProjectV2ItemFieldValue(
            input: {
              projectId: $projectId
              itemId: $itemId
              fieldId: $fieldId
              value: {
                singleSelectOptionId: $optionId
              }
            }
          ) {
            projectV2Item {
              id
            }
          }
        }
        """

        variables = {
            "projectId": project_id,
            "itemId": item_id,
            "fieldId": field_id,
            "optionId": option_id,
        }
        self.execute_graphql(mutation, variables)

    def find_linked_issues(self, pr_body: str, pr_title: str = "") -> list[int]:
        """Find linked issues from PR body and title."""
        text = f"{pr_title} {pr_body}".lower()
        pattern = r"(?:fix(?:es)?|close(?:s)?|resolve(?:s)?)\s+#(\d+)"
        matches = re.findall(pattern, text, re.IGNORECASE)
        return [int(match) for match in matches]

    def determine_target_status(self, event_name: str, action: str, **kwargs) -> str | None:
        """Determine target status based on event."""
        if event_name == "issues":
            if action == "opened":
                return "Backlog"
            elif action == "assigned":
                return "Ready"
            elif action == "closed":
                return "Done"
            elif action == "reopened":
                return "Backlog"
        elif event_name == "pull_request":
            is_draft = kwargs.get("is_draft", False)
            is_merged = kwargs.get("is_merged", False)

            if action == "opened":
                return "In progress" if is_draft else "In review"
            elif action == "ready_for_review":
                return "In review"
            elif action == "converted_to_draft":
                return "In progress"
            elif action == "closed" and is_merged:
                return "Done"

        return None

    def update_issue_status(
        self, issue_number: int, target_status: str, should_add_to_project: bool = False
    ) -> None:
        """Update project status for an issue."""
        print(f"🎯 Processing Issue #{issue_number} → '{target_status}'")

        try:
            # Get issue and project items
            issue = self.get_issue_project_items(issue_number)
            print(f"✅ Found issue: {issue['title']}")

            project_items = issue["projectItems"]["nodes"]

            # Add to project if needed and not in any projects
            if not project_items and should_add_to_project:
                print(f"🔗 Adding issue to Project #{self.project_number}...")
                self.add_issue_to_project(issue_number)

                # Refresh project items
                issue = self.get_issue_project_items(issue_number)
                project_items = issue["projectItems"]["nodes"]

            if not project_items:
                print("ℹ️ Issue is not in any projects, skipping status update")
                return

            # Update status in each project
            for project_item in project_items:
                project = project_item["project"]
                print(f"📊 Processing project: {project['title']} (#{project['number']})")

                try:
                    # Get Status field
                    status_field = self.get_project_status_field(project["id"])

                    # Find target status option
                    status_option = next(
                        (opt for opt in status_field["options"] if opt["name"] == target_status),
                        None,
                    )

                    if not status_option:
                        available_options = [opt["name"] for opt in status_field["options"]]
                        print(f"⚠️ Status option '{target_status}' not found")
                        print(f"Available options: {', '.join(available_options)}")
                        continue

                    print(f"🎯 Using status option: {status_option['name']}")

                    # Update project item status
                    self.update_project_item_status(
                        project["id"], project_item["id"], status_field["id"], status_option["id"]
                    )

                    print(f"✅ Updated project {project['title']}: Status set to '{target_status}'")

                except Exception as e:
                    print(f"❌ Error updating project {project['title']}: {e}")
                    continue

        except Exception as e:
            print(f"❌ Error updating project status: {e}")
            raise

    def handle_issue_event(
        self, issue_number: int, action: str, assignees: list[str] | None = None
    ) -> None:
        """Handle issue events."""
        assignees = assignees or []
        print(f"🔍 Processing issue #{issue_number} - action: {action}")
        print(f"👥 Assignees: {', '.join(assignees) if assignees else 'none'}")

        target_status = self.determine_target_status("issues", action)

        if not target_status:
            print("ℹ️ No status change needed")
            return

        # Only add to project for opened events
        should_add_to_project = action == "opened"
        self.update_issue_status(issue_number, target_status, should_add_to_project)

    def handle_pr_event(
        self,
        pr_number: int,
        action: str,
        pr_body: str,
        pr_title: str = "",
        is_draft: bool = False,
        is_merged: bool = False,
    ) -> None:
        """Handle pull request events."""
        print(f"🔍 Processing PR #{pr_number} - action: {action}")

        # Find linked issues
        linked_issues = self.find_linked_issues(pr_body, pr_title)

        if not linked_issues:
            print("ℹ️ No linked issues found")
            return

        print(f"🔗 Linked issues: {', '.join(map(str, linked_issues))}")

        target_status = self.determine_target_status(
            "pull_request", action, is_draft=is_draft, is_merged=is_merged
        )

        if not target_status:
            print("ℹ️ No status change needed")
            return

        # Update each linked issue
        for issue_number in linked_issues:
            self.update_issue_status(issue_number, target_status)


def main() -> None:
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(description="Update GitHub Project v2 status")
    parser.add_argument("--event-name", choices=["issues", "pull_request"], required=True)
    parser.add_argument("--action", required=True)
    parser.add_argument("--issue-number", type=int, help="Issue number (for issue events)")
    parser.add_argument("--pr-number", type=int, help="PR number (for PR events)")
    parser.add_argument("--pr-body", help="PR body text")
    parser.add_argument("--pr-title", help="PR title")
    parser.add_argument("--is-draft", action="store_true", help="PR is draft")
    parser.add_argument("--is-merged", action="store_true", help="PR is merged")
    parser.add_argument("--assignees", nargs="*", default=[], help="Issue assignees")
    parser.add_argument("--project-owner", help="Project owner (default: ApprenticeGC)")
    parser.add_argument("--project-number", type=int, default=3, help="Project number")

    args = parser.parse_args()

    # Get repository info from environment
    github_repo = os.getenv("GITHUB_REPOSITORY", "")
    if "/" not in github_repo:
        print("❌ Could not determine repository from GITHUB_REPOSITORY")
        sys.exit(1)

    owner, repo = github_repo.split("/", 1)
    project_owner = args.project_owner or "ApprenticeGC"

    # Get token from environment
    token = os.getenv("GITHUB_TOKEN")
    if not token:
        print("❌ GITHUB_TOKEN not found")
        sys.exit(1)

    print(f"🚀 Starting project status update for {owner}/{repo}")
    print(f"📊 Project: {project_owner}/#{args.project_number}")

    try:
        updater = GitHubProjectUpdater(token, owner, repo, project_owner, args.project_number)

        if args.event_name == "issues":
            if not args.issue_number:
                print("❌ --issue-number required for issue events")
                sys.exit(1)
            updater.handle_issue_event(args.issue_number, args.action, args.assignees)

        elif args.event_name == "pull_request":
            if not args.pr_number or not args.pr_body:
                print("❌ --pr-number and --pr-body required for PR events")
                sys.exit(1)
            updater.handle_pr_event(
                args.pr_number,
                args.action,
                args.pr_body,
                args.pr_title or "",
                args.is_draft,
                args.is_merged,
            )

        print("✅ Project status update completed successfully")

    except Exception as e:
        print(f"❌ Script failed: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
