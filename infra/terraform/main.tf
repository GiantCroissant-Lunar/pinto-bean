provider "github" {
  owner = var.github_owner
}

data "github_repository" "this" {
  name = var.repository_name
}
