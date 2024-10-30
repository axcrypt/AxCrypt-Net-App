function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        console.log('Copied to clipboard successfully!');
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