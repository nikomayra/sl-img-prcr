# Bizarre Gallery

## Description:

Serverless image processor using a Azure Function App and Azure's blob storage. Users can upload images tagged with start/middle/end from the frontend. Function App takes 3 uploaded images (randomly 1 each of start/middle/end tagged images), generates a Gif, and assigns a random title. On load of the static frontend the last 20 gifs are loaded from the server.

Just a tech demo showing some serverless image processing architecture turned into a fun little art project.

## Tech Stack:

**Frontend:** Simple static html/js/css <br>
**Backend:** Azure Function App <br>
**Database:** Azure Blob Storage <br>
**Deployment:** Github Pages for frontend <br>

## How to use
1. Upload an image following image requirements. <br>
2. If at least 1 start, middle and end images exist in storage on upload a Gif is created. <br>
3. On page load Gif gallery is loaded in (including any new ones created since last page load). <br>

## Key Features:
* NSFWJS client side model for basic image filtering.
* Azure Function App, Blob Storage and API Management rate limiting.
* Imagesharp library for Gif creation, image resizing.
* Image extension and MIME type validation.
* Embedded resource .txt file with 250 random unique Titles.

<img width="599" alt="fe" src="https://github.com/user-attachments/assets/7cec45c7-e1c1-4893-a94b-2285146f68ca">
