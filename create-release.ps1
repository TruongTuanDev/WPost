param(
    [string]$Version = "1.0.0"
)

$Tag = "v$($Version.TrimStart('v'))"
Write-Host "Creating and pushing Git Tag: $Tag..." -ForegroundColor Cyan

git add .
git commit -m "chore(release): bump version to $Tag" --allow-empty
git tag -a $Tag -m "Release $Tag"
git push origin main
git push origin $Tag

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Tag $Tag has been pushed to GitHub!" -ForegroundColor Green
Write-Host "GitHub Actions is now building and publishing the release." -ForegroundColor Green
Write-Host "Once finished, your direct download link will be:" -ForegroundColor Yellow
Write-Host "https://github.com/TruongTuanDev/WPost/releases/download/$Tag/WPost.exe" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Green
