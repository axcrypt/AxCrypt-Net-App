function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        const copiedMessageElement = document.getElementById("copiedMessage");
        if (copiedMessageElement) {
            copiedMessageElement.style.display = "block"; // Show the message
            setTimeout(() => {
                copiedMessageElement.style.display = "none"; // Hide after 2 seconds
            }, 2000);
        }
    }, function (err) {
        console.error('Could not copy text: ', err);
    });
}
function addClickListenerOutside(dotNetHelper, inputId) {
    document.addEventListener('click', function (event) {
        var input = document.getElementById(inputId);
        if (!input) return;
        var withinBoundaries = event.composedPath().includes(input);

        if (!withinBoundaries) {
            dotNetHelper.invokeMethodAsync('HideField', inputId);
        }
    });
}