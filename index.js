let model;
let uploading = false;
let messages = [];

// Load
window.onload = async () => {
  model = await nsfwjs.load("./model.json");
  console.log("Model Loaded");
  await loadGallery();
};

// ---------------------------------------

async function loadGallery() {
  const gallery = document.getElementById("gallery");
  gallery.innerHTML = "";

  try {
    const response = await fetch("https://sl-img-prcr.azurewebsites.net/api/GetLastGifs");
    if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);

    const imageUrls = await response.json();
    imageUrls.forEach((url) => {
      const fig = document.createElement("figure");
      const img = document.createElement("img");
      const figCap = document.createElement("figcaption");
      const gifTitle = url.split("_")[1].replace(".gif", "").replace(/-/g, " ");
      figCap.innerText = gifTitle;
      img.src = url;
      img.alt = "Generated GIF";
      img.classList.add("gallery-item");
      fig.append(img, figCap);
      gallery.appendChild(fig);
    });
    console.log("Gallery Loaded");
  } catch (error) {
    console.error("Error loading gallery: ", error);
  }
}

document.getElementById("uploadForm").addEventListener("submit", async function (event) {
  event.preventDefault();

  if (uploading) {
    showMessage("An upload is already in progress. Please wait.", "error");
    return;
  }

  let formData = new FormData();
  let fileInput = document.getElementById("imageUpload");
  let position = document.querySelector('input[name="position"]:checked').value;

  if (!fileInput.files || fileInput.files.length === 0) {
    showMessage("Please add an image file.", "error");
    return;
  }

  // Validate image dimensions, size, type and content
  let isValid = await validateImage(fileInput.files[0]);
  if (!isValid) return;

  formData.append("image", fileInput.files[0]);
  formData.append("position", position); // Add the position metadata

  uploading = true;
  document.querySelector('button[type="submit"]').disabled = true;

  await postData(formData);

  document.querySelector('button[type="submit"]').disabled = false;
  uploading = false;
});

async function postData(formData) {
  const url = "https://sl-img-prcr.azurewebsites.net/api/PostImage";
  try {
    showMessage("Uploading, please wait...", "info");
    const response = await fetch(url, {
      method: "POST",
      body: formData,
      enctype: "multipart/form-data",
    });
    if (!response.ok) {
      throw new Error(`Response status: ${response.status}`);
    }
    // Success message
    showMessage("Image uploaded successfully!", "success");
  } catch (error) {
    console.error(error.message);
    // Display error message
    showMessage(error.message, "error");
  }
}

function validateImage(file) {
  // Check file size (1MB limit)
  if (file.size > 1 * 1024 * 1024) {
    //alert('File size must be less than 1 MB.');
    showMessage("File size must be less than 1 MB.", "error");
    return false;
  }

  // Check file type
  const allowedTypes = ["image/jpeg", "image/png", "image/tiff", "image/bmp"];
  if (!allowedTypes.includes(file.type)) {
    //alert('Invalid file type. Please upload a JPEG, PNG, TIFF, or BMP.');
    showMessage("Invalid file type. Please upload a JPEG, PNG, TIFF, or BMP.", "error");
    return false;
  }

  return new Promise((resolve) => {
    const img = new Image();
    img.src = URL.createObjectURL(file);

    img.onload = async () => {
      const aspectRatio = img.width / img.height;
      console.log(`W: ${img.width} H: ${img.height} AS: ${aspectRatio}`);
      if (aspectRatio < 0.9 && aspectRatio > 1.1) {
        // Allow 10% off-square images through.
        showMessage("Image aspect ratio must  be 1:1", "error");
        resolve(false);
      } else if (img.width < 128) {
        showMessage("Image dimensions must be >= 128x128 pixels.", "error");
        resolve(false);
      } else {
        const predictions = await model.classify(img);
        console.log("Predicitions: ", predictions);
        const isSafe = predictions.every(
          (p) => (p.className !== "Porn" && p.className !== "Hentai") || p.probability < 0.6
        );

        if (!isSafe) {
          showMessage("NSFW content detected. Image cannot be uploaded.", "error");
          resolve(false);
        } else {
          resolve(true); // Image is safe for upload
        }
      }
    };
  });
}

function showMessage(msg, type) {
  const messageList = document.getElementById("messageList");

  const messageItem = document.createElement("li");
  messageItem.classList.add("message-item", type);
  messageItem.textContent = msg;

  messageList.prepend(messageItem);

  messages.unshift(msg);
  if (messages.length > 4) {
    messages.pop();
    messageList.removeChild(messageList.lastChild);
  }

  setTimeout(() => {
    messageItem.remove();
  }, 150000);
}
