# Setting Up Your Own Spotify App for **Fantasy Player**

Due to recent changes in how spotify provides access to it's API you will now need to provide your own client ID to continue to use Fantasy Player.

Fantasy Player integrates with Spotify to enhance your experience, but you’ll need to create your own Spotify application in order to obtain a **Client ID**. This guide walks you through the process step-by-step.

---

## 1. Create a Spotify Developer Account
1. Visit the Spotify Developer Dashboard: https://developer.spotify.com/dashboard
2. Log in with your Spotify account.
3. If prompted, accept the Developer Terms.

---

## 2. Create a New Spotify App
1. In the **Dashboard**, click **"Create app"**.
2. Enter the following:
    - **App Name:** Anything you like (e.g., *Fantasy Player Integration*).
    - The redirect URI should be http://127.0.0.1:2984/callback
    - **App Description:** Optional.
    - Select the Web API checkbox under the `Which API/SDKs are you planning to use?` header 
3. Confirm that you agree to the terms.
4. Click **Create**.

---

## 3. Retrieve Your Client ID
Once the app is created:
1. Open your newly created app.
2. On the **App Overview** page, you will see:
    - **Client ID** → This is what Fantasy Player needs.
    - **Client Secret** → You do *not* need this.

Copy the **Client ID**.

---

## 4. Add Your Client ID to Fantasy Player
1. Type in /pfp config to open Fantasy Player's configuration window.
2. Find the header "Spotify Settings" and open it
3. Locate the field labeled **"Spotify Client ID"**.
4. Paste your copied Client ID into the field.
5. Hit save.

---

## 5. That's It!
Fantasy Player will now let you pick Spotify as a provider and login(you will need to authenticate with your newly created app)

If you ever regenerate your Client ID or create a new Spotify app, just update the field inside the Fantasy Player configuration.

## Troubleshoting
If you put in the wrong Spotify Client ID, put in the correct one, hit save and then reload the plugin.

