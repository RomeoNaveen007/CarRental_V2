function previewImage(event) {
    const reader = new FileReader();
    reader.onload = function () {
        const output = document.getElementById("imgPreview");
        output.src = reader.result;
        output.style.display = "block";
    };
    reader.readAsDataURL(event.target.files[0]);
}

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("driverForm");
    if (form) {
        form.addEventListener("submit", function () {
            // Custom JS validation hook (expand as needed)
            console.log("Driver form submitted!");
        });
    }
});

