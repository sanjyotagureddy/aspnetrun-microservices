# Tooling recommendations

- Add `.editorconfig` to enforce formatting and basic naming rules.
- Centralize package versions and analyzers in `Directory.Build.props`.
- Use `git-secrets` or a credential scanner to block secrets.
- Use `reportgenerator` to convert coverage to HTML and upload as CI artifact.
