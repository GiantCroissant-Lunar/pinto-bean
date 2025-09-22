"""Tests for code analyzer module."""

import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent.parent))

import pytest  # noqa: F401
from ai_review.code_analyzer import CodeAnalyzer, FeedbackType


class TestCodeAnalyzer:
    """Test cases for CodeAnalyzer."""

    def setup_method(self) -> None:
        """Setup test instance."""
        self.analyzer = CodeAnalyzer(".")

    def test_bug_fix_detection(self) -> None:
        """Test bug fix detection."""
        comment = "This code has a bug that causes crashes"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.BUG_FIX  # noqa: S101

    def test_feedback_improvement(self) -> None:
        """Test improvement detection."""
        comment = "This could be optimized for better performance"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.IMPROVEMENT  # noqa: S101

    def test_feedback_test_request(self) -> None:
        """Test test request detection."""
        comment = "Please add unit tests for this function"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.TEST_REQUEST  # noqa: S101

    def test_feedback_documentation(self) -> None:
        """Test documentation request detection."""
        comment = "This needs better documentation and comments"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.DOCUMENTATION  # noqa: S101

    def test_feedback_security(self) -> None:
        """Test security concern detection."""
        comment = "This code is vulnerable to injection attacks"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.SECURITY  # noqa: S101

    def test_feedback_performance(self) -> None:
        """Test performance concern detection."""
        comment = "This is too slow and uses too much memory"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.PERFORMANCE  # noqa: S101

    def test_feedback_general(self) -> None:
        """Test general feedback fallback."""
        comment = "Random comment that doesn't fit patterns"
        result = self.analyzer.analyze_feedback(comment)
        assert result == FeedbackType.GENERAL  # noqa: S101

    def test_modifications_bug_fix(self) -> None:
        """Test bug fix suggestions."""
        suggestions = self.analyzer.suggest_modifications(
            FeedbackType.BUG_FIX, "Fix this bug", "test.py", 10
        )
        assert len(suggestions) >= 1  # noqa: S101
        assert suggestions[0]["type"] == "add_comment"  # noqa: S101

    def test_modifications_test_request(self) -> None:
        """Test test creation suggestions."""
        suggestions = self.analyzer.suggest_modifications(
            FeedbackType.TEST_REQUEST, "Add tests", "src/module.py", 15
        )
        assert len(suggestions) >= 1  # noqa: S101
        assert suggestions[0]["type"] == "create_test"  # noqa: S101

    def test_truncate_comment(self) -> None:
        """Test comment truncation."""
        long_comment = "A" * 100
        truncated = self.analyzer._truncate_comment(long_comment, 50)
        assert len(truncated) <= 50  # noqa: S101
        assert truncated.endswith("...")  # noqa: S101

    def test_get_test_file_path(self) -> None:
        """Test test file path generation."""
        # Python file
        test_path = self.analyzer._get_test_file_path("src/module.py")
        assert "test_module.py" in test_path  # noqa: S101
        assert "tests" in test_path  # noqa: S101

        # Already a test file
        test_path = self.analyzer._get_test_file_path("test_existing.py")
        assert "test_existing.py" in test_path  # noqa: S101
