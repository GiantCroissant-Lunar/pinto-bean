variable "github_owner" {
	type        = string
	description = "GitHub organization or user name that owns the repository"
}

variable "repository_name" {
	type        = string
	description = "Target GitHub repository name to manage"
	default     = "pinto-bean"
}

variable "default_branch" {
	type        = string
	description = "Main (protected) branch name"
	default     = "main"
}

variable "repository_secrets" {
	description = "Map of GitHub Actions repository secrets to create (name => value). Sensitive; set in Terraform Cloud workspace as HCL or environment vars."
	type        = map(string)
	default     = {}
	sensitive   = true
}

variable "repository_variables" {
	description = "Map of GitHub Actions repository variables to create (name => value)."
	type        = map(string)
	default     = {}
}

variable "environments" {
	description = "Optional map of environment names to objects with secrets and variables ( { env = { secrets = {K=V}, variables = {K=V} } } )."
	type = map(object({
		secrets   = optional(map(string), {})
		variables = optional(map(string), {})
	}))
	default   = {}
	sensitive = true
}

variable "required_status_checks" {
	description = "List of required status check contexts for branch protection"
	type        = list(string)
	default     = []
}

variable "require_signed_commits" {
	type        = bool
	description = "Whether to require signed commits on protected branch"
	default     = true
}

variable "required_approving_review_count" {
	type        = number
	description = "Pull request reviews required before merging"
	default     = 1
}

variable "dismiss_stale_reviews" {
	type        = bool
	description = "Dismiss stale PR reviews when new commits push"
	default     = true
}

variable "require_code_owner_reviews" {
	type        = bool
	description = "Require code owner reviews"
	default     = false
}

variable "enforce_admins" {
	type        = bool
	description = "Include admins in branch protection rules"
	default     = true
}

variable "allow_force_pushes" {
	type        = bool
	description = "Allow force pushes on protected branch"
	default     = false
}

variable "allow_deletions" {
	type        = bool
	description = "Allow deletion of the protected branch"
	default     = false
}

variable "lock_branch" {
	type        = bool
	description = "Lock the branch"
	default     = false
}
