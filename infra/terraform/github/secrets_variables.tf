resource "github_actions_secret" "repository" {
  for_each        = var.repository_secrets
  repository      = var.repository_name
  secret_name     = each.key
  plaintext_value = each.value
}

resource "github_actions_variable" "repository" {
  for_each      = var.repository_variables
  repository    = var.repository_name
  variable_name = each.key
  value         = each.value
}

resource "github_repository_environment" "env" {
  for_each    = var.environments
  repository  = var.repository_name
  environment = each.key
}

locals {
  environment_secret_pairs = flatten([
    for env_name, cfg in var.environments : [
      for sk, sv in lookup(cfg, "secrets", {}) : {
        env   = env_name
        name  = sk
        value = sv
      }
    ]
  ])

  environment_variable_pairs = flatten([
    for env_name, cfg in var.environments : [
      for vk, vv in lookup(cfg, "variables", {}) : {
        env   = env_name
        name  = vk
        value = vv
      }
    ]
  ])
}

resource "github_actions_environment_secret" "env_pair" {
  for_each        = { for p in local.environment_secret_pairs : "${p.env}:${p.name}" => p }
  repository      = var.repository_name
  environment     = github_repository_environment.env[each.value.env].environment
  secret_name     = each.value.name
  plaintext_value = each.value.value
}

resource "github_actions_environment_variable" "env_pair" {
  for_each      = { for p in local.environment_variable_pairs : "${p.env}:${p.name}" => p }
  repository    = var.repository_name
  environment   = github_repository_environment.env[each.value.env].environment
  variable_name = each.value.name
  value         = each.value.value
}
