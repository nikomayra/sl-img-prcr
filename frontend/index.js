// Load in NSFW filter upfront
let model;
window.onload = async () => {
  model = await nsfwjs.load('./model.json');
  console.log('Model Loaded');
};

// ---------------------------------------

document
  .getElementById('uploadForm')
  .addEventListener('submit', async function (event) {
    event.preventDefault();

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

    await postData(formData);
  });

async function postData(formData) {
  const url = 'http://localhost:7071/api/PostImage';
  try {
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
      console.log(`W: ${img.width} H: ${img.height}`);
      if (img.width !== 250 || img.height !== 250) {
        message('Image dimensions must be 250x250 pixels.', 'error');
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
          message('Image Validated.', 'success');
          resolve(true); // Image is safe for upload
        }
      }
    };
  });
}

function message(message, type) {
  let messageContainer;

  if (type === 'success') {
    messageContainer = document.getElementById('successMessage');
  } else if (type === 'error') {
    messageContainer = document.getElementById('errorMessage');
  }

  // Append the new message as a new line
  messageContainer.innerHTML += `<div>${message}</div>`;

  // Clear messages after 8 seconds
  setTimeout(() => {
    messageContainer.innerHTML = '';
  }, 10000);
}
