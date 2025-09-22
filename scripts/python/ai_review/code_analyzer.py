"""Code analysis and modification utilities."""

import logging
import re
from enum import Enum
from pathlib import Path
from typing import Any

logger = logging.getLogger(__name__)


class FeedbackType(Enum):
    """Types of review feedback."""

    BUG_FIX = "bug_fix"
    IMPROVEMENT = "improvement"
    TEST_REQUEST = "test_request"
    DOCUMENTATION = "documentation"
    SECURITY = "security"
    PERFORMANCE = "performance"
    GENERAL = "general"


class CodeAnalyzer:
    """Analyzes review comments and suggests code modifications."""

    def __init__(self, repo_root: str = "."):
        """Initialize with repository root path."""
        self.repo_root = Path(repo_root)
        self.feedback_patterns = {
            FeedbackType.BUG_FIX: [
                r"fix.*bug",
                r"error",
                r"issue",
                r"broken",
                r"doesn't work",
                r"crash",
                r"exception",
                r"fails?",
                r"incorrect",
            ],
            FeedbackType.IMPROVEMENT: [
                r"improve",
                r"optimize",
                r"refactor",
                r"better",
                r"cleaner",
                r"simplify",
                r"enhance",
                r"efficient",
            ],
            FeedbackType.TEST_REQUEST: [
                r"test",
                r"coverage",
                r"unit test",
                r"integration test",
                r"spec",
                r"verify",
                r"validate",
            ],
            FeedbackType.DOCUMENTATION: [
                r"document",
                r"comment",
                r"explain",
                r"clarify",
                r"doc",
                r"readme",
                r"example",
                r"usage",
            ],
            FeedbackType.SECURITY: [
                r"security",
                r"vulnerable",
                r"exploit",
                r"sanitize",
                r"validate input",
                r"injection",
                r"xss",
            ],
            FeedbackType.PERFORMANCE: [
                r"performance",
                r"slow",
                r"optimize",
                r"efficient",
                r"memory",
                r"cpu",
                r"bottleneck",
            ],
        }

    def analyze_feedback(self, comment_body: str) -> FeedbackType:
        """Analyze comment to determine feedback type."""
        comment_lower = comment_body.lower()

        # Check each feedback type's patterns
        for feedback_type, patterns in self.feedback_patterns.items():
            for pattern in patterns:
                if re.search(pattern, comment_lower):
                    logger.info(f"Detected feedback type: {feedback_type.value}")
                    return feedback_type

        logger.info("No specific feedback type detected, using general")
        return FeedbackType.GENERAL

    def suggest_modifications(
        self,
        feedback_type: FeedbackType,
        comment_body: str,
        file_path: str,
        line_number: int | None = None,
    ) -> list[dict[str, Any]]:
        """Suggest code modifications based on feedback."""
        suggestions = []

        if feedback_type == FeedbackType.BUG_FIX:
            suggestions.extend(self._suggest_bug_fixes(comment_body, file_path, line_number))
        elif feedback_type == FeedbackType.IMPROVEMENT:
            suggestions.extend(self._suggest_improvements(comment_body, file_path, line_number))
        elif feedback_type == FeedbackType.TEST_REQUEST:
            suggestions.extend(self._suggest_tests(comment_body, file_path, line_number))
        elif feedback_type == FeedbackType.DOCUMENTATION:
            suggestions.extend(self._suggest_documentation(comment_body, file_path, line_number))
        elif feedback_type == FeedbackType.SECURITY:
            suggestions.extend(self._suggest_security_fixes(comment_body, file_path, line_number))
        elif feedback_type == FeedbackType.PERFORMANCE:
            suggestions.extend(
                self._suggest_performance_improvements(comment_body, file_path, line_number)
            )
        else:
            suggestions.extend(self._suggest_general_response(comment_body, file_path, line_number))

        return suggestions

    def _suggest_bug_fixes(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Suggest bug fix modifications."""
        suggestions = []

        if line_number and self._file_exists(file_path):
            # Add TODO comment for manual review
            suggestions.append(
                {
                    "type": "add_comment",
                    "file": file_path,
                    "line": line_number,
                    "content": f"# TODO: Fix bug - {self._truncate_comment(comment)}",
                    "description": "Added TODO comment for bug fix",
                }
            )

            # If it's a Python file, suggest adding error handling
            if file_path.endswith(".py"):
                suggestions.append(
                    {
                        "type": "add_error_handling",
                        "file": file_path,
                        "line": line_number,
                        "content": "# Consider adding try-catch block for error handling",
                        "description": "Suggested error handling improvement",
                    }
                )

        return suggestions

    def _suggest_improvements(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Suggest code improvements."""
        suggestions = []

        if line_number and self._file_exists(file_path):
            suggestions.append(
                {
                    "type": "add_comment",
                    "file": file_path,
                    "line": line_number,
                    "content": f"# IMPROVE: {self._truncate_comment(comment)}",
                    "description": "Added improvement suggestion comment",
                }
            )

        return suggestions

    def _suggest_tests(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Suggest test additions."""
        suggestions = []

        # Determine test file path
        test_file = self._get_test_file_path(file_path)

        suggestions.append(
            {
                "type": "create_test",
                "file": test_file,
                "content": self._generate_test_template(file_path, line_number, comment),
                "description": f"Created test case for {file_path}",
            }
        )

        return suggestions

    def _suggest_documentation(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Suggest documentation improvements."""
        suggestions = []

        if line_number and self._file_exists(file_path):
            suggestions.append(
                {
                    "type": "add_comment",
                    "file": file_path,
                    "line": line_number,
                    "content": f"# DOC: {self._truncate_comment(comment)}",
                    "description": "Added documentation note",
                }
            )

        return suggestions

    def _suggest_security_fixes(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Suggest security improvements."""
        suggestions = []

        if line_number and self._file_exists(file_path):
            suggestions.append(
                {
                    "type": "add_comment",
                    "file": file_path,
                    "line": line_number,
                    "content": f"# SECURITY: {self._truncate_comment(comment)}",
                    "description": "Added security concern note",
                }
            )

        return suggestions

    def _suggest_performance_improvements(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Suggest performance improvements."""
        suggestions = []

        if line_number and self._file_exists(file_path):
            suggestions.append(
                {
                    "type": "add_comment",
                    "file": file_path,
                    "line": line_number,
                    "content": f"# PERFORMANCE: {self._truncate_comment(comment)}",
                    "description": "Added performance improvement note",
                }
            )

        return suggestions

    def _suggest_general_response(
        self, comment: str, file_path: str, line_number: int | None
    ) -> list[dict[str, Any]]:
        """Handle general feedback."""
        suggestions = []

        # Log the feedback for manual review
        log_file = self.repo_root / ".ai-review-log"
        suggestions.append(
            {
                "type": "log_feedback",
                "file": str(log_file),
                "content": f"Review feedback for {file_path}:{line_number or 'N/A'} - {comment}",
                "description": "Logged general feedback for manual review",
            }
        )

        return suggestions

    def _file_exists(self, file_path: str) -> bool:
        """Check if file exists."""
        return (self.repo_root / file_path).exists()

    def _truncate_comment(self, comment: str, max_length: int = 80) -> str:
        """Truncate comment to fit in code."""
        if len(comment) <= max_length:
            return comment
        return comment[: max_length - 3] + "..."

    def _get_test_file_path(self, source_file: str) -> str:
        """Generate test file path from source file."""
        path = Path(source_file)
        stem = path.stem
        suffix = path.suffix

        # Common test file patterns
        if "test" not in stem.lower():
            test_name = f"test_{stem}{suffix}"
        else:
            test_name = f"{stem}{suffix}"

        # Place in tests directory
        if path.parts[0] == "src":
            test_path = Path("tests") / Path(*path.parts[1:-1]) / test_name
        else:
            test_path = Path("tests") / path.parent / test_name

        return str(test_path)

    def _generate_test_template(
        self, source_file: str, line_number: int | None, comment: str
    ) -> str:
        """Generate test template based on source file type."""
        file_path = Path(source_file)

        if file_path.suffix == ".py":
            return self._generate_python_test_template(source_file, line_number, comment)
        elif file_path.suffix in [".js", ".ts"]:
            return self._generate_js_test_template(source_file, line_number, comment)
        else:
            return self._generate_generic_test_template(source_file, line_number, comment)

    def _generate_python_test_template(
        self, source_file: str, line_number: int | None, comment: str
    ) -> str:
        """Generate Python test template."""
        module_name = Path(source_file).stem
        return f'''"""Test cases for {source_file}."""

import pytest
from {module_name} import *  # noqa


class Test{module_name.title()}:
    """Test cases addressing review feedback."""

    def test_{module_name}_line_{line_number or "unknown"}(self):
        """Test case for review comment: {self._truncate_comment(comment, 50)}"""
        # TODO: Implement test based on review feedback
        # Original comment: {comment}
        # File: {source_file}:{line_number or "N/A"}
        pass

    def test_{module_name}_edge_cases(self):
        """Test edge cases."""
        # TODO: Add edge case tests
        pass
'''

    def _generate_js_test_template(
        self, source_file: str, line_number: int | None, comment: str
    ) -> str:
        """Generate JavaScript/TypeScript test template."""
        module_name = Path(source_file).stem
        return f"""/**
 * Test cases for {source_file}
 */

describe('{module_name}', () => {{
  it('should handle review feedback from line {line_number or "unknown"}', () => {{
    // TODO: Implement test based on review feedback
    // Original comment: {comment}
    // File: {source_file}:{line_number or "N/A"}
  }});

  it('should handle edge cases', () => {{
    // TODO: Add edge case tests
  }});
}});
"""

    def _generate_generic_test_template(
        self, source_file: str, line_number: int | None, comment: str
    ) -> str:
        """Generate generic test template."""
        return f"""# Test cases for {source_file}

# Test case addressing review feedback
# Original comment: {comment}
# File: {source_file}:{line_number or "N/A"}

# TODO: Implement appropriate tests for this file type
"""
