"""AI Review Response System.

This module provides automated response to GitHub PR review comments,
including code analysis, modifications, and conversation resolution.
"""

__version__ = "1.0.0"
__author__ = "AI Review Bot"

from .code_analyzer import CodeAnalyzer
from .core import ReviewResponseHandler
from .github_client import GitHubClient

__all__ = ["ReviewResponseHandler", "GitHubClient", "CodeAnalyzer"]
