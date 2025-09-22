import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "scripts" / "python"))

from validator_core import calc_entropy, extract_high_entropy_tokens, is_template_json  # noqa: E402


def test_entropy_bounds() -> None:
    assert calc_entropy("") == 0.0
    low = calc_entropy("aaaaaaaaaa")
    high = calc_entropy("abcdef012345")
    assert low < high


def test_extract_high_entropy_tokens() -> None:
    tokens = extract_high_entropy_tokens(
        "short abcdefghijklmnopqrstuvwxyz0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789== end"
    )
    assert any(len(t[0]) >= 20 for t in tokens)


def test_template_detection() -> None:
    assert is_template_json("infra/terraform/secrets/terraform.json")
    assert is_template_json("infra/terraform/github/secrets/github-vars.json")
    assert not is_template_json("infra/terraform/secrets/other.json")
