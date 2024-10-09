document
  .getElementById('uploadForm')
  .addEventListener('submit', async function (event) {
    event.preventDefault();

    let formData = new FormData();
    let fileInput = document.getElementById('imageUpload');
    let position = document.querySelector(
      'input[name="position"]:checked'
    ).value;

    // Validate image dimensions, size, and type
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

  // Check dimensions
  const img = new Image();
  img.src = URL.createObjectURL(file);

  // Use a promise to handle the image load event
  return new Promise((resolve) => {
    img.onload = () => {
      if (img.width != 250 || img.height != 250) {
        //alert('Image dimensions must be 250x250 pixels.');
        message('Image dimensions must be 250x250 pixels.', 'error');
        resolve(false);
      } else {
        resolve(true);
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
