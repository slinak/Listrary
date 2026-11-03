This application is designed to create, manage, and share lists.  Lists can be for tasks, groceries, wishlists, or another type.

Lists can be shared directly via URL or via Circles, where other users can join and share their lists to all members of the Circle.

## Azure deployment

Before deploying, add a GitHub Actions secret named `LISTMANAGER_CONNECTION_STRING` containing the production SQL Server connection string. The deployment workflows apply it to the App Service as the `DefaultConnection` connection string. Keep the value in GitHub Secrets; do not commit it to the repository.
