resource "github_branch_protection" "main" {
  repository_id = data.github_repository.this.node_id
  pattern       = var.default_branch

  enforce_admins       = var.enforce_admins
  allows_deletions     = var.allow_deletions
  allows_force_pushes  = var.allow_force_pushes
  lock_branch          = var.lock_branch

  required_pull_request_reviews {
    required_approving_review_count = var.required_approving_review_count
    dismiss_stale_reviews           = var.dismiss_stale_reviews
    require_code_owner_reviews      = var.require_code_owner_reviews
  }

  required_status_checks {
    strict   = true
    contexts = var.required_status_checks
  }

  require_signed_commits = var.require_signed_commits
}
