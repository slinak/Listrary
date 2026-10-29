# Listrary for Android

Native Android client (Kotlin + Jetpack Compose) for the Listrary list / task / grocery / wishlist manager, with list sharing and "Circles" groups. It is a **standalone Gradle project** that lives in the `android/` folder and is independent of the .NET solution: open `android/` (not the repo root) in Android Studio.

> The app talks to the REST API under `/api/v1` that is being added to the ASP.NET Core backend (contract below). All Entra ID / tenant / client / host values in this project that say `REPLACE_WITH_...` are **placeholders** and must be replaced before sign-in or deep links will work.

## Prerequisites

- Android Studio Koala (2024.1) or newer (AGP 8.5.x)
- JDK 17 (the JDK bundled with Android Studio is fine)
- Android SDK Platform 34 (compileSdk / targetSdk 34, minSdk 26)
- Kotlin 1.9.24 with Compose compiler 1.5.14 (pinned in `gradle/libs.versions.toml`)

The Gradle wrapper (`gradlew`, `gradlew.bat`, `gradle/wrapper/`) is checked in, pinned to Gradle 8.7:

```powershell
cd android
./gradlew assembleDebug      # build a debug APK
./gradlew installDebug       # install on a connected device/emulator
```

Or just open `android/` in Android Studio and press Run.

## Configuration (all placeholders by default)

Everything configurable lives in `android/gradle.properties` (override any value in `~/.gradle/gradle.properties` or with `-Plistrary.xxx=...` to keep real values out of source control). The values are surfaced to code through `BuildConfig` and manifest placeholders.

| Property | Purpose | Default |
|---|---|---|
| `listrary.debugBaseUrl` | API base URL for debug builds (`BuildConfig.BASE_URL`) | `https://10.0.2.2:5001/api/v1/` (host machine from the emulator) |
| `listrary.releaseBaseUrl` | API base URL for release builds | `https://REPLACE_WITH_PROD_HOST/api/v1/` |
| `listrary.deepLinkHost` | Host for the https App Link | `REPLACE_WITH_PROD_HOST` |
| `listrary.tenantId` | Entra ID tenant | `4ed55fae-09dd-4c7f-9ea2-74cf47c83e38` |
| `listrary.androidClientId` | Client ID of the **Android public-client** app registration | `REPLACE_WITH_ANDROID_CLIENT_ID` |
| `listrary.apiScope` | Scope exposed by the backend API registration | `api://REPLACE_WITH_API_APP_ID/access_as_user` |
| `listrary.signatureHash` | Base64 signature hash of the app signing key | `REPLACE_WITH_BASE64_SIGNATURE_HASH` |

To point the app at a different backend, change `listrary.debugBaseUrl` / `listrary.releaseBaseUrl` (must end with `/`). The debug default uses HTTPS on `10.0.2.2:5001`, which presents the ASP.NET dev certificate; the emulator does not trust it by default, so either install/trust the dev cert on the emulator or use a plain-HTTP URL (cleartext also requires a debug-only network security config, not included).

### Entra ID / MSAL setup

Sign-in uses MSAL (`com.microsoft.identity.client:msal`) in single-account mode. `auth/MsalAuthManager.kt` generates the MSAL configuration at runtime from the BuildConfig values above (redirect URI = `msauth://com.listrary.android/<url-encoded signature hash>`).

1. **Backend API registration** (the existing app registration used by the web app / API): under *Expose an API*, set the Application ID URI (e.g. `api://<api-client-id>`) and add a delegated scope `access_as_user`. Put `api://<api-client-id>/access_as_user` in `listrary.apiScope`.
2. **New Android registration**: *App registrations → New registration* in tenant `4ed55fae-09dd-4c7f-9ea2-74cf47c83e38` (placeholder - verify it is yours). Under *Authentication → Add a platform → Android* enter package name `com.listrary.android` and the signature hash of your debug/release keystore (`keytool -exportcert -alias <alias> -keystore <keystore> | openssl sha1 -binary | openssl base64`). The portal shows the resulting redirect URI `msauth://com.listrary.android/<hash>`.
3. Copy the registration's *Application (client) ID* into `listrary.androidClientId`, and the Base64 hash (not URL-encoded) into `listrary.signatureHash` (it is also used for the `BrowserTabActivity` intent filter in `AndroidManifest.xml`).
4. Under *API permissions* on the Android registration, add the `access_as_user` delegated permission from the backend API and grant consent if required.

The backend must validate Entra ID access tokens for that audience.

## REST API contract

Base path `/api/v1`, JSON, `Authorization: Bearer <Entra ID access token>` on all endpoints. Requests that carry a `shareToken` query parameter (list view and item operations) are also sent without a bearer token when the user is not signed in, so an anonymous share link can still open.

- `GET /lists` -> `{ circleGroups: [{ circle: {circleId,name}, lists: [ListSummary] }], otherLists: [ListSummary] }`
- `GET /lists/{id}?shareToken=` -> `{ list, items, canEdit, shareToken, activeShares }`
- `POST /lists`, `PUT /lists/{id}` `{ listName, listType, circleId, strictStoreLocations }` -> `ListSummary`; `DELETE /lists/{id}`
- `POST /lists/{id}/share` `{ permission: "View"|"Edit" }` -> `{ listShare, shareLink }`
- `POST /lists/{listId}/items`, `PUT /lists/{listId}/items/{itemId}` -> `{ item, warning? }`; `DELETE .../items/{itemId}`
- `POST /lists/{listId}/items/{itemId}/toggle` -> `ListItem`; `POST /lists/{listId}/items/mark-all-incomplete`
- `GET /lists/{listId}/items/suggestions/names?query=` and `.../suggestions/locations?itemName=&query=` -> `string[]`
- `GET /circles` -> `{ circles, pendingInvitations }`; `POST /circles`; `GET /circles/{id}`; `DELETE /circles/{id}`
- `POST /circles/{id}/invite`, `DELETE /circles/{id}/members/{userId}`, `POST /circles/{id}/leave`
- `POST /circles/invitations/{id}/accept|reject`, `DELETE /circles/invitations/{id}`

Enums (`listType`, `permission`) are exchanged as strings (`Shopping|ToDo|WishList|Other`, `View|Edit`). The DTOs are in `data/remote/dto/`. Assumed (not in the contract): incoming `pendingInvitations` may carry an optional `circleName` for display.

## Deep links / sharing

The share URL used by the web app, `https://REPLACE_WITH_PROD_HOST/Lists/Manage/{listId}?shareToken={token}`, opens the List Detail screen directly (verified App Link for the `listrary.deepLinkHost` host; the host's `/.well-known/assetlinks.json` must list the app's package and signing certificate). Fallback custom scheme: `listrary://list/{listId}?shareToken={token}`. The token is sent with every request for that list, and the screen honours the API's `canEdit` flag (View links hide all edit controls). Test with:

```powershell
adb shell am start -a android.intent.action.VIEW -d "listrary://list/1?shareToken=abc"
```

## Architecture

MVVM + Repository, single `:app` module, package `com.listrary.android`:

```
auth/            MsalAuthManager (MSAL single-account wrapper), AuthState
data/remote/     Retrofit ListraryApiService, kotlinx.serialization DTOs, AuthInterceptor, NetworkModule (Hilt)
data/local/      Room AppDatabase, entities, DAOs, DatabaseModule (Hilt)
data/repository/ Lists / ListItems / Circles repositories (interface + Impl), mappers
di/              RepositoryModule (Hilt @Binds)
ui/              Compose screens + @HiltViewModels: auth, lists, listdetail, circles, share, settings,
                 common (shared composables), navigation (Navigation Compose graph + deep links), theme
```

- **Offline-first:** the UI only observes Room `Flow`s. Repositories call the API and replace the cached rows on success (`refresh*`), so cached data stays visible when offline; failures surface as snackbars/error states.
- **Auth:** `AuthInterceptor` attaches a silently-acquired MSAL token to every call (none when signed out, allowing share-token access). Sign-out also clears the Room cache.
- **Ordering:** List Detail shows incomplete items first (grouped by store location for Shopping lists, no-location last), then completed items, mirroring the web app.
- **Localization:** English (`values/`) and Spanish (`values-es/`), switchable in Settings via `AppCompatDelegate.setApplicationLocales`. Messages mirror the backend's `StoreLocations.*` and `Circles.Status.*` keys.

## Known limitations / open decisions

- Offline sync is a simple "replace on successful fetch"; mutations require connectivity (no queued offline writes, no conflict resolution).
- Localization is limited to English and Spanish.
- The deep link host, Entra ID client/API IDs, signature hash and production base URL are placeholders pending the production domain and real app registrations.
- The server's warning/error text is not displayed; the app shows its own localized `store_locations_*` messages (a 4xx when adding to a strict list with a location is treated as the strict-location error).
- The API exposes only user IDs for circle members/invitees, so members are listed by ID.
- No app icon or automated tests are included yet; the project was scaffolded without running a Gradle build, so expect to fix minor compile issues on first sync.
