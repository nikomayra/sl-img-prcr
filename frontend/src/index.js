document
  .getElementById('uploadForm')
  .addEventListener('submit', async function (event) {
    event.preventDefault();

    let formData = new FormData();
    let fileInput = document.getElementById('imageUpload');
    let position = document.querySelector(
      'input[name="position"]:checked'
    ).value;

    formData.append('image', fileInput.files[0]);
    formData.append('position', position); // Add the position metadata

    // Upload logic here - you can use fetch or another library to send this to your backend or Azure Function
    postData(formData);
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
    console.error('Successfully posted data.');
  } catch (error) {
    console.error(error.message);
  }
}
