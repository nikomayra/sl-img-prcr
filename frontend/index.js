let model;
let uploading = false;
let messages = [];

// Load
window.onload = async () => {
  model = await nsfwjs.load('./model.json');
  console.log('Model Loaded');
  await loadGallery();
};

// ---------------------------------------

async function loadGallery() {
  const gallery = document.getElementById('gallery');
  gallery.innerHTML = '';

  try {
    const response = await fetch('http://localhost:7071/api/GetLastGifs');
    if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);

    const imageUrls = await response.json();
    imageUrls.forEach((url) => {
      const fig = document.createElement('figure');
      const img = document.createElement('img');
      const figCap = document.createElement('figcaption');
      const gifTitle = url.split('_')[1].replace('.gif', '').replace(/-/g, ' ');
      figCap.innerText = gifTitle;
      img.src = url;
      img.alt = 'Generated GIF';
      img.classList.add('gallery-item');
      fig.append(img, figCap);
      gallery.appendChild(fig);
    });
    console.log('Gallery Loaded');
  } catch (error) {
    console.error('Error loading gallery: ', error);
  }
}

document
  .getElementById('uploadForm')
  .addEventListener('submit', async function (event) {
    event.preventDefault();

    if (uploading) {
      message('An upload is already in progress. Please wait.', 'error');
      return;
    }

    let formData = new FormData();
    let fileInput = document.getElementById('imageUpload');
    let position = document.querySelector(
      'input[name="position"]:checked'
    ).value;

    if (!fileInput.files || fileInput.files.length === 0) {
      message('Please add an image file.', 'error');
      return;
    }

    // Validate image dimensions, size, type and content
    if (!validateImage(fileInput.files[0])) return;

    formData.append('image', fileInput.files[0]);
    formData.append('position', position); // Add the position metadata

    uploading = true;
    document.querySelector('button[type="submit"]').disabled = true;

    await postData(formData);

    document.querySelector('button[type="submit"]').disabled = false;
    uploading = false;
  });

async function postData(formData) {
  const url = 'http://localhost:7071/api/PostImage';
  try {
    message('Uploading, please wait...', 'success');
    const response = await fetch(url, {
      method: 'POST',
      body: formData,
      enctype: 'multipart/form-data',
    });
    if (!response.ok) {
      throw new Error(`Response status: ${response.status}`);
    }
    // Success message
    message('Image uploaded successfully!', 'success');
  } catch (error) {
    console.error(error.message);
    // Display error message
    message(error.message, 'error');
  }
}

function validateImage(file) {
  // Check file size (1MB limit)
  if (file.size > 1 * 1024 * 1024) {
    //alert('File size must be less than 1 MB.');
    message('File size must be less than 1 MB.', 'error');
    return false;
  }

  // Check file type
  const allowedTypes = ['image/jpeg', 'image/png', 'image/tiff', 'image/bmp'];
  if (!allowedTypes.includes(file.type)) {
    //alert('Invalid file type. Please upload a JPEG, PNG, TIFF, or BMP.');
    message(
      'Invalid file type. Please upload a JPEG, PNG, TIFF, or BMP.',
      'error'
    );
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
        message('Image aspect ratio must  be 1:1', 'error');
        resolve(false);
      } else if (img.width < 128) {
        message('Image dimensions must be >= 128x128 pixels.', 'error');
        resolve(false);
      } else {
        const predictions = await model.classify(img);
        console.log('Predicitions: ', predictions);
        const isSafe = predictions.every(
          (p) =>
            (p.className !== 'Porn' && p.className !== 'Hentai') ||
            p.probability < 0.5
        );

        if (!isSafe) {
          message('NSFW content detected. Image cannot be uploaded.', 'error');
          resolve(false);
        } else {
          resolve(true); // Image is safe for upload
        }
      }
    };
  });
}

function message(msg, type) {
  let messageContainer;

  if (type === 'success') {
    messageContainer = document.getElementById('successMessage');
  } else if (type === 'error') {
    messageContainer = document.getElementById('errorMessage');
  }

  // Maintain message history with a maximum of 4 messages
  messages.unshift(msg); // Add the new message to the front
  if (messages.length > 4) {
    messages.pop(); // Remove the oldest message if limit exceeded
  }

  // Clear current messages and display updated history
  messageContainer.innerHTML = messages
    .map((message) => `<div>${message}</div>`)
    .join('');

  // Clear messages after seconds
  setTimeout(() => {
    messageContainer.innerHTML = '';
  }, 30000);
}
