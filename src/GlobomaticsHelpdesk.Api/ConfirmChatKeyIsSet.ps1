if ([string]::IsNullOrWhiteSpace($env:ChatProvider__ApiKey)) {
    "Not set in this terminal"
} else {
    "Set in this terminal"
}