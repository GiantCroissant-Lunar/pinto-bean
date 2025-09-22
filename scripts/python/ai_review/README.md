# AI Review Response System

An intelligent system for automatically responding to GitHub PR review comments, making appropriate code changes, and resolving conversations.

## Features

- 🤖 **Smart Comment Analysis**: Automatically categorizes review feedback into types (bug fixes, improvements, tests, documentation, security, performance)
- 🔧 **Automated Code Changes**: Makes contextual modifications based on feedback type
- 💬 **Conversation Resolution**: Automatically resolves review threads after addressing feedback
- 🧪 **Test Generation**: Creates test templates when test coverage is requested
- 📝 **Documentation**: Adds appropriate comments and documentation notes
- 🔍 **Bulk Operations**: Process multiple review threads at once

## Architecture

```
ai_review/
├── __init__.py          # Package initialization
├── cli.py               # Command-line interface
├── core.py              # Main review response handler
├── github_client.py     # GitHub API client using gh CLI
├── code_analyzer.py     # Comment analysis and code modification logic
├── tests/               # Test suite
└── README.md           # This file
```

## Usage

### Command Line Interface

```bash
# Process a single review comment
python -m ai_review.cli process-comment 123 456 "Please add error handling" --file src/app.py --line 42

# Bulk resolve threads by author
python -m ai_review.cli bulk-resolve 123 --author github-copilot

# Resolve all unresolved threads
python -m ai_review.cli bulk-resolve 123 --unresolved-only
```

### GitHub Workflow Integration

The system is designed to work with GitHub Actions. See `.github/workflows/review-response.yml` for the complete workflow that:

1. Triggers on review comments
2. Analyzes the feedback
3. Makes appropriate code changes
4. Commits the changes
5. Resolves the conversation

### Python API

```python
from ai_review.core import ReviewResponseHandler

handler = ReviewResponseHandler("owner/repo", ".")

# Process a review comment
result = handler.process_review_comment(
    pr_number=123,
    comment_id=456,
    comment_body="Please add error handling here",
    file_path="src/app.py",
    line_number=42
)

# Bulk resolve threads
result = handler.bulk_resolve_threads(
    pr_number=123,
    author_filter="github-copilot"
)
```

## Feedback Types

The system recognizes these types of review feedback:

- **Bug Fix**: Comments about errors, crashes, or incorrect behavior
- **Improvement**: Suggestions for optimization, refactoring, or enhancement
- **Test Request**: Requests for additional test coverage
- **Documentation**: Requests for comments, docs, or examples
- **Security**: Security concerns or vulnerabilities
- **Performance**: Performance optimization suggestions
- **General**: Any other feedback

## Code Modifications

Based on the feedback type, the system can:

### Bug Fixes
- Add TODO comments for manual review
- Suggest error handling patterns
- Insert debugging comments

### Improvements
- Add improvement notes
- Insert refactoring suggestions

### Test Requests
- Generate test file templates
- Create test cases with proper structure
- Support Python, JavaScript/TypeScript, and generic test patterns

### Documentation
- Add documentation comments
- Insert usage examples
- Create README sections

### Security
- Add security review notes
- Insert input validation reminders

### Performance
- Add performance optimization notes
- Insert profiling suggestions

## Configuration

The system uses these environment variables:

- `GITHUB_REPOSITORY`: Repository name (auto-detected from git remote)
- `GITHUB_TOKEN`: GitHub token for API access (provided by GitHub Actions)

## Testing

Run the test suite:

```bash
cd scripts/python
pytest ai_review/tests/ -v
```

## Requirements

- Python 3.11+
- GitHub CLI (`gh`) installed and authenticated
- Git repository with GitHub remote
- Required Python packages (see `requirements-dev.txt`)

## Integration with Workflows

This system integrates with the existing workflow structure:

1. **Pre-commit hooks** ensure code quality
2. **CodeQL** performs security analysis
3. **Trivy** scans for vulnerabilities
4. **AI Review Response** handles automated feedback responses

The AI system complements manual code review by handling routine feedback and freeing up reviewers to focus on higher-level concerns.
