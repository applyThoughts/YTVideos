This project lists all videos from in a youtube channel and export to csv. Uses the [Google API](https://developers.google.com/youtube/v3/docs "Google API") to fetch all videos in the channel. 

The YouTube API gives you access to YouTubes data in a more comprehensive, scalable way . You can retrieve entire playlists, users’ uploads, and even search results using the YouTube API. You can also add YouTube functionalities to your website so that users can upload videos and manage channel subscriptions straight from your website or app.

 Log in to [Google Developers Console](https://console.cloud.google.com/apis/dashboard "Google Developers Console") using your Google account. Once logged in create a project and enable YouTube Data API v3.
After enabling create a credential to obtain an api key,

The solution has two projects. One a console app and another a website. Built using asp.net core and c#. Both projects essentialy does the same thing. The core api calls and csv export is handled in the YTVideos.Core project.

Feel free to explore and play with it.
