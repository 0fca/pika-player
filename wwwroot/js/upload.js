document.getElementById("file_uploads").addEventListener("change", () => {
    const messageLabel = document.getElementById("preview");
    const fileList = document.getElementById("filesList");

    messageLabel.innerText = "Size: "+returnFileSize(getSummarySize());
    const uploadButton = document.getElementById("upload_button");

    if (getSummarySize() === 0) {
        uploadButton.setAttribute("disabled", true);
    }

    if (getSummarySize() >= 50 * 1048576) {
        messageLabel.innerText = "File or files are too big to upload it at a time.";
        uploadButton.setAttribute("disabled", true);
        //deleteAllFiles();

    } else if (uploadButton.hasAttribute("disabled")) {
        uploadButton.removeAttribute("disabled");
    }
    const list = getFileList();
    list.forEach(entry => {
        const li = document.createElement("li");
        li.innerText = entry;
        li.setAttribute("class","collection-item");
        fileList.appendChild(li);
})
})
document.getElementById("delete_all_files").addEventListener("click", () => {
    deleteAllFiles();
    const uploadButton = document.getElementById("upload_button");
    uploadButton.setAttribute("disabled", true);
})
document.getElementById("upload_button").addEventListener("click", () =>{
    const uploadSection = document.getElementById("upload_modal");
    uploadSection.removeAttribute("hidden");
})