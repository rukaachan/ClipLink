from __future__ import annotations


def build_prompt(template: str, image_path: str) -> str:
    if not image_path or not image_path.strip():
        raise ValueError("image_path is required")
    if "{path}" not in template:
        raise ValueError("prompt_template must contain {path}")
    return template.replace("{path}", image_path)
