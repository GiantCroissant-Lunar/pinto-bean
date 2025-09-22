"""Core review response handler."""

import logging
import subprocess
from datetime import datetime
from pathlib import Path
from typing import Any

from .code_analyzer import CodeAnalyzer, FeedbackType
from .github_client import GitHubClient

logger = logging.getLogger(__name__)


class ReviewResponseHandler:
    """Main handler for processing review comments and responses."""

    def __init__(self, repo: str, repo_root: str = "."):
        """Initialize the review response handler."""
        self.repo = repo
        self.repo_root = Path(repo_root)
        self.github_client = GitHubClient(repo)
        self.code_analyzer = CodeAnalyzer(repo_root)

    def process_review_comment(
        self,
        pr_number: int,
        comment_id: int,
        comment_body: str,
        file_path: str | None = None,
        line_number: int | None = None,
    ) -> dict[str, Any]:
        """Process a review comment and generate response."""
        logger.info(f"Processing review comment {comment_id} on PR #{pr_number}")

        result = {
            "success": False,
            "comment_id": comment_id,
            "pr_number": pr_number,
            "actions_taken": [],
            "thread_resolved": False,
            "error": None,
        }

        try:
            # Analyze the feedback
            feedback_type = self.code_analyzer.analyze_feedback(comment_body)
            logger.info(f"Detected feedback type: {feedback_type.value}")

            # Get modification suggestions
            suggestions = self.code_analyzer.suggest_modifications(
                feedback_type, comment_body, file_path or "", line_number
            )

            # Apply modifications
            applied_changes = []
            for suggestion in suggestions:
                if self._apply_suggestion(suggestion):
                    applied_changes.append(suggestion)

            result["actions_taken"] = applied_changes

            # Commit changes if any were made
            if applied_changes:
                commit_message = self._generate_commit_message(
                    feedback_type, comment_body, file_path, line_number
                )
                if self._commit_changes(commit_message):
                    logger.info("Changes committed successfully")
                else:
                    logger.warning("Failed to commit changes")

            # Add response comment
            response_body = self._generate_response_comment(
                feedback_type, comment_body, applied_changes
            )
            self.github_client.add_pr_comment(pr_number, response_body)

            # Resolve conversation if we made meaningful changes
            if applied_changes and feedback_type != FeedbackType.GENERAL:
                thread_id = self.github_client.find_thread_by_comment_id(pr_number, comment_id)
                if thread_id:
                    if self.github_client.resolve_review_thread(thread_id):
                        result["thread_resolved"] = True
                        logger.info(f"Resolved conversation thread {thread_id}")

            result["success"] = True

        except Exception as e:
            logger.error(f"Error processing review comment: {e}")
            result["error"] = str(e)

        return result

    def _apply_suggestion(self, suggestion: dict[str, Any]) -> bool:
        """Apply a single modification suggestion."""
        try:
            suggestion_type = suggestion["type"]
            file_path = Path(suggestion["file"])

            if suggestion_type == "add_comment":
                return self._add_code_comment(
                    file_path, int(suggestion.get("line", 1)), suggestion["content"]
                )
            elif suggestion_type == "create_test":
                return self._create_test_file(file_path, suggestion["content"])
            elif suggestion_type == "log_feedback":
                return self._log_feedback(file_path, suggestion["content"])
            elif suggestion_type == "add_error_handling":
                return self._add_error_handling_comment(
                    file_path, int(suggestion.get("line", 1)), suggestion["content"]
                )
            else:
                logger.warning(f"Unknown suggestion type: {suggestion_type}")
                return False

        except Exception as e:
            logger.error(f"Failed to apply suggestion {suggestion}: {e}")
            return False

    def _add_code_comment(self, file_path: Path, line_number: int, comment: str) -> bool:
        """Add a comment to a specific line in a file."""
        try:
            if not file_path.exists():
                logger.warning(f"File {file_path} does not exist")
                return False

            # Read file content
            with open(file_path, encoding="utf-8") as f:
                lines = f.readlines()

            # Insert comment at specified line (1-indexed)
            if 1 <= line_number <= len(lines):
                # Determine indentation from the target line
                target_line = lines[line_number - 1] if line_number <= len(lines) else ""
                indent = len(target_line) - len(target_line.lstrip())
                indented_comment = " " * indent + comment + "\n"

                lines.insert(line_number - 1, indented_comment)

                # Write back to file
                with open(file_path, "w", encoding="utf-8") as f:
                    f.writelines(lines)

                logger.info(f"Added comment to {file_path}:{line_number}")
                return True
            else:
                logger.warning(f"Line number {line_number} out of range for {file_path}")
                return False

        except Exception as e:
            logger.error(f"Failed to add comment to {file_path}: {e}")
            return False

    def _create_test_file(self, file_path: Path, content: str) -> bool:
        """Create a test file with the given content."""
        try:
            # Ensure directory exists
            file_path.parent.mkdir(parents=True, exist_ok=True)

            # Write test content
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content)

            logger.info(f"Created test file: {file_path}")
            return True

        except Exception as e:
            logger.error(f"Failed to create test file {file_path}: {e}")
            return False

    def _log_feedback(self, file_path: Path, content: str) -> bool:
        """Log feedback to a file."""
        try:
            timestamp = datetime.now().isoformat()
            log_entry = f"[{timestamp}] {content}\n"

            with open(file_path, "a", encoding="utf-8") as f:
                f.write(log_entry)

            logger.info(f"Logged feedback to {file_path}")
            return True

        except Exception as e:
            logger.error(f"Failed to log feedback to {file_path}: {e}")
            return False

    def _add_error_handling_comment(self, file_path: Path, line_number: int, comment: str) -> bool:
        """Add error handling suggestion comment."""
        return self._add_code_comment(file_path, line_number, comment)

    def _commit_changes(self, message: str) -> bool:
        """Commit changes to git."""
        try:
            # Check if there are changes to commit
            result = subprocess.run(  # noqa: S603
                ["git", "diff", "--quiet"],  # noqa: S607
                cwd=self.repo_root,
                capture_output=True,
            )

            if result.returncode == 0:
                logger.info("No changes to commit")
                return True

            # Add all changes
            subprocess.run(["git", "add", "."], cwd=self.repo_root, check=True)  # noqa: S603,S607

            # Commit changes
            subprocess.run(["git", "commit", "-m", message], cwd=self.repo_root, check=True)  # noqa: S603,S607

            logger.info("Changes committed successfully")
            return True

        except subprocess.CalledProcessError as e:
            logger.error(f"Git operation failed: {e}")
            return False

    def _generate_commit_message(
        self,
        feedback_type: FeedbackType,
        comment_body: str,
        file_path: str | None,
        line_number: int | None,
    ) -> str:
        """Generate commit message for the changes."""
        truncated_comment = comment_body[:100] + "..." if len(comment_body) > 100 else comment_body

        location = f"{file_path}:{line_number}" if file_path and line_number else "General"

        return f"""AI Response: {feedback_type.value.replace('_', ' ').title()}

Review comment: {truncated_comment}
Location: {location}

🤖 Generated by AI review response system"""

    def _generate_response_comment(
        self,
        feedback_type: FeedbackType,
        original_comment: str,
        actions_taken: list[dict[str, Any]],
    ) -> str:
        """Generate response comment for the PR."""
        response_parts = [
            "🤖 **AI Review Response**",
            "",
            f"**Feedback Type Detected**: {feedback_type.value.replace('_', ' ').title()}",
            "",
            "**Actions Taken**:",
        ]

        if actions_taken:
            for action in actions_taken:
                response_parts.append(f"- {action.get('description', 'Unknown action')}")
        else:
            response_parts.append("- Acknowledged feedback (no automated changes made)")

        response_parts.extend(
            [
                "",
                "**Original Comment**:",
                f"> {original_comment}",
                "",
                "---",
                "*This is an automated response. The conversation will be marked as resolved if changes were made.*",
            ]
        )

        return "\n".join(response_parts)

    def bulk_resolve_threads(
        self, pr_number: int, author_filter: str | None = None, resolved_filter: bool | None = None
    ) -> dict[str, Any]:
        """Bulk resolve review threads based on filters."""
        logger.info(f"Bulk resolving threads for PR #{pr_number}")

        result: dict[str, Any] = {
            "total_threads": 0,
            "resolved_count": 0,
            "failed_count": 0,
            "errors": [],
        }

        try:
            threads = self.github_client.get_pr_review_threads(pr_number)
            result["total_threads"] = len(threads)

            for thread in threads:
                # Apply filters
                if resolved_filter is not None and thread.is_resolved != resolved_filter:
                    continue

                if author_filter:
                    thread_authors = [comment["author"] for comment in thread.comments]
                    if author_filter not in thread_authors:
                        continue

                # Resolve the thread
                if self.github_client.resolve_review_thread(thread.id):
                    result["resolved_count"] += 1
                else:
                    result["failed_count"] += 1
                    result["errors"].append(f"Failed to resolve thread {thread.id}")

        except Exception as e:
            logger.error(f"Error in bulk resolve: {e}")
            result["errors"].append(str(e))

        return result
