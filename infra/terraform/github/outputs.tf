output "repository_full_name" {
  value       = data.github_repository.this.full_name
  description = "Full name of the managed repository"
}

output "managed_environments" {
  value       = keys(var.environments)
  description = "List of environment names managed"
}
