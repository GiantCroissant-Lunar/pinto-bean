"""Core logic extracted from validate_secrets for unit testing.
Provides entropy calculation and helper predicates.
"""
from __future__ import annotations
import math, re

HIGH_ENTROPY_THRESHOLD = 4.0
MIN_SECRET_LENGTH = 20
secret_token_re = re.compile(r"([A-Za-z0-9+/=_-]{20,})")

def calc_entropy(s: str) -> float:
    from collections import Counter
    if not s:
        return 0.0
    c = Counter(s)
    n = len(s)
    return -sum((cnt/n) * math.log2(cnt/n) for cnt in c.values())

def extract_high_entropy_tokens(text: str):
    results = []
    for token in secret_token_re.findall(text or ""):
        if len(token) < MIN_SECRET_LENGTH:
            continue
        ent = calc_entropy(token)
        if ent >= HIGH_ENTROPY_THRESHOLD:
            results.append((token, ent))
    return results

def is_template_json(path: str) -> bool:
    path = path.replace('\\','/')
    return path in {"infra/terraform/secrets/terraform.json", "infra/terraform/github/secrets/github-vars.json"}
