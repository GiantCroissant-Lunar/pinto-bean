"""GitHub API client for handling PR interactions."""

import json
import logging
import subprocess
from dataclasses import dataclass
from typing import Any

logger = logging.getLogger(__name__)


@dataclass
class ReviewThread:
    """Represents a GitHub review thread."""

    id: str
    is_resolved: bool
    is_outdated: bool
    comments: list[dict[str, Any]]


@dataclass
class PRComment:
    """Represents a PR review comment."""

    id: str
    database_id: int
    body: str
    path: str
    line: int | None
    author: str
    thread_id: str | None = None


class GitHubClient:
    """GitHub API client using gh CLI."""

    def __init__(self, repo: str):
        """Initialize with repository name (owner/repo)."""
        self.repo = repo
        self.owner, self.name = repo.split("/")

    def _run_gh_command(self, command: list[str]) -> str:
        """Run gh CLI command and return output."""
        try:
            result = subprocess.run(  # noqa: S603
                ["gh", *command],
                capture_output=True,
                text=True,
                check=True,  # noqa: S607
            )
            return result.stdout.strip()
        except subprocess.CalledProcessError as e:
            logger.error(f"GitHub CLI command failed: {e.stderr}")
            raise

    def _run_graphql_query(self, query: str, variables: dict[str, Any]) -> dict[str, Any]:
        """Execute GraphQL query using gh CLI."""
        cmd = ["api", "graphql"]

        # Add variables as arguments
        for key, value in variables.items():
            if isinstance(value, int):
                cmd.extend(["-F", f"{key}={value}"])
            else:
                cmd.extend(["-f", f"{key}={value}"])

        cmd.extend(["-f", f"query={query}"])

        result = self._run_gh_command(cmd)
        return json.loads(result)

    def get_pr_review_threads(self, pr_number: int) -> list[ReviewThread]:
        """Get all review threads for a PR."""
        query = """
        query($owner: String!, $name: String!, $number: Int!) {
          repository(owner: $owner, name: $name) {
            pullRequest(number: $number) {
              reviewThreads(first: 100) {
                nodes {
                  id
                  isResolved
                  isOutdated
                  comments(first: 20) {
                    nodes {
                      id
                      databaseId
                      author { login }
                      body
                      path
                      line
                    }
                  }
                }
              }
            }
          }
        }
        """

        variables = {"owner": self.owner, "name": self.name, "number": pr_number}

        response = self._run_graphql_query(query, variables)
        threads_data = response["data"]["repository"]["pullRequest"]["reviewThreads"]["nodes"]

        threads = []
        for thread_data in threads_data:
            comments = []
            for comment_data in thread_data["comments"]["nodes"]:
                comments.append(
                    {
                        "id": comment_data["id"],
                        "database_id": comment_data["databaseId"],
                        "author": comment_data["author"]["login"],
                        "body": comment_data["body"],
                        "path": comment_data.get("path"),
                        "line": comment_data.get("line"),
                    }
                )

            threads.append(
                ReviewThread(
                    id=thread_data["id"],
                    is_resolved=thread_data["isResolved"],
                    is_outdated=thread_data["isOutdated"],
                    comments=comments,
                )
            )

        return threads

    def find_thread_by_comment_id(self, pr_number: int, comment_id: int) -> str | None:
        """Find thread ID containing a specific comment."""
        threads = self.get_pr_review_threads(pr_number)

        for thread in threads:
            for comment in thread.comments:
                if comment["database_id"] == comment_id:
                    return thread.id

        return None

    def resolve_review_thread(self, thread_id: str) -> bool:
        """Resolve a review thread conversation."""
        mutation = """
        mutation($threadId: ID!) {
          resolveReviewThread(input: {threadId: $threadId}) {
            thread {
              id
              isResolved
            }
          }
        }
        """

        variables = {"threadId": thread_id}

        try:
            self._run_graphql_query(mutation, variables)
            logger.info(f"Successfully resolved thread {thread_id}")
            return True
        except Exception as e:
            logger.error(f"Failed to resolve thread {thread_id}: {e}")
            return False

    def unresolve_review_thread(self, thread_id: str) -> bool:
        """Unresolve a review thread conversation."""
        mutation = """
        mutation($threadId: ID!) {
          unresolveReviewThread(input: {threadId: $threadId}) {
            thread {
              id
              isResolved
            }
          }
        }
        """

        variables = {"threadId": thread_id}

        try:
            self._run_graphql_query(mutation, variables)
            logger.info(f"Successfully unresolved thread {thread_id}")
            return True
        except Exception as e:
            logger.error(f"Failed to unresolve thread {thread_id}: {e}")
            return False

    def add_pr_comment(self, pr_number: int, body: str) -> bool:
        """Add a comment to the PR."""
        try:
            self._run_gh_command(["pr", "comment", str(pr_number), "--body", body])
            logger.info(f"Added comment to PR #{pr_number}")
            return True
        except Exception as e:
            logger.error(f"Failed to add comment to PR #{pr_number}: {e}")
            return False

    def get_pr_files(self, pr_number: int) -> list[str]:
        """Get list of files changed in the PR."""
        try:
            result = self._run_gh_command(
                ["pr", "view", str(pr_number), "--json", "files", "-q", ".files[].path"]
            )
            return result.split("\n") if result else []
        except Exception as e:
            logger.error(f"Failed to get PR files: {e}")
            return []
