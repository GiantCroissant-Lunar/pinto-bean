#!/usr/bin/env python3
"""Command-line interface for AI review response system."""

import argparse
import logging
import os
import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from ai_review.core import ReviewResponseHandler


def setup_logging(level: str = "INFO") -> None:
    """Setup logging configuration."""
    logging.basicConfig(
        level=getattr(logging, level.upper()),
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )


def get_repo_name() -> str:
    """Get repository name from environment or git remote."""
    # Try environment variable first
    repo = os.getenv("GITHUB_REPOSITORY")
    if repo:
        return repo

    # Try to get from git remote
    try:
        import subprocess

        result = subprocess.run(  # noqa: S603
            ["git", "remote", "get-url", "origin"],  # noqa: S607
            capture_output=True,
            text=True,
            check=True,
        )
        url = result.stdout.strip()

        # Parse GitHub URL
        if "github.com" in url:
            # Handle both SSH and HTTPS URLs
            if url.startswith("git@"):
                # git@github.com:owner/repo.git
                repo_part = url.split(":")[-1]
            else:
                # https://github.com/owner/repo.git
                repo_part = "/".join(url.split("/")[-2:])

            # Remove .git suffix
            if repo_part.endswith(".git"):
                repo_part = repo_part[:-4]

            return repo_part
    except Exception as e:
        logging.debug(f"Failed to get repo from git remote: {e}")

    raise ValueError(
        "Could not determine repository name. Set GITHUB_REPOSITORY environment variable."
    )


def cmd_process_comment(args: argparse.Namespace) -> None:
    """Process a single review comment."""
    handler = ReviewResponseHandler(args.repo, args.repo_root)

    result = handler.process_review_comment(
        pr_number=args.pr_number,
        comment_id=args.comment_id,
        comment_body=args.comment_body,
        file_path=args.file_path,
        line_number=args.line_number,
    )

    if result["success"]:
        print("✅ Review comment processed successfully")
        print(f"Actions taken: {len(result['actions_taken'])}")
        print(f"Thread resolved: {result['thread_resolved']}")
    else:
        print(f"❌ Failed to process comment: {result['error']}")
        sys.exit(1)


def cmd_bulk_resolve(args: argparse.Namespace) -> None:
    """Bulk resolve review threads."""
    handler = ReviewResponseHandler(args.repo, args.repo_root)

    result = handler.bulk_resolve_threads(
        pr_number=args.pr_number,
        author_filter=args.author_filter,
        resolved_filter=False if args.unresolved_only else None,
    )

    print("📊 Bulk resolve results:")
    print(f"Total threads: {result['total_threads']}")
    print(f"Resolved: {result['resolved_count']}")
    print(f"Failed: {result['failed_count']}")

    if result["errors"]:
        print("\nErrors:")
        for error in result["errors"]:
            print(f"  - {error}")


def main() -> None:
    """Main CLI entry point."""
    parser = argparse.ArgumentParser(
        description="AI Review Response System",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Process a single review comment
  %(prog)s process-comment 123 456 "Please add error handling" --file src/app.py --line 42

  # Bulk resolve threads by author
  %(prog)s bulk-resolve 123 --author github-copilot

  # Resolve all unresolved threads
  %(prog)s bulk-resolve 123 --unresolved-only
        """,
    )

    parser.add_argument(
        "--repo", help="Repository name (owner/repo). Auto-detected if not provided."
    )
    parser.add_argument(
        "--repo-root", default=".", help="Repository root directory (default: current directory)"
    )
    parser.add_argument(
        "--log-level",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        default="INFO",
        help="Logging level (default: INFO)",
    )

    subparsers = parser.add_subparsers(dest="command", help="Available commands")

    # Process comment command
    process_parser = subparsers.add_parser(
        "process-comment", help="Process a single review comment"
    )
    process_parser.add_argument("pr_number", type=int, help="Pull request number")
    process_parser.add_argument("comment_id", type=int, help="Review comment ID")
    process_parser.add_argument("comment_body", help="Review comment body text")
    process_parser.add_argument("--file", dest="file_path", help="File path being reviewed")
    process_parser.add_argument(
        "--line", dest="line_number", type=int, help="Line number being reviewed"
    )
    process_parser.set_defaults(func=cmd_process_comment)

    # Bulk resolve command
    bulk_parser = subparsers.add_parser("bulk-resolve", help="Bulk resolve review threads")
    bulk_parser.add_argument("pr_number", type=int, help="Pull request number")
    bulk_parser.add_argument("--author-filter", help="Only resolve threads by this author")
    bulk_parser.add_argument(
        "--unresolved-only", action="store_true", help="Only process unresolved threads"
    )
    bulk_parser.set_defaults(func=cmd_bulk_resolve)

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        sys.exit(1)

    # Setup logging
    setup_logging(args.log_level)

    # Auto-detect repo if not provided
    if not args.repo:
        try:
            args.repo = get_repo_name()
            logging.info(f"Auto-detected repository: {args.repo}")
        except ValueError as e:
            print(f"Error: {e}")
            sys.exit(1)

    # Execute command
    try:
        args.func(args)
    except KeyboardInterrupt:
        print("\n🛑 Interrupted by user")
        sys.exit(1)
    except Exception as e:
        logging.error(f"Unexpected error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
