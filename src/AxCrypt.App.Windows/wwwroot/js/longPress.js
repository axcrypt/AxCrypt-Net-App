function addLongPressListener(elementId, dotNetHelper) {
    let timer;
    const element = document.getElementById(elementId);
    if (!element) return;
    element.addEventListener('mousedown', function (event) {
        timer = setTimeout(function () {
            // Call the Blazor method on long press
            dotNetHelper.invokeMethodAsync('OnLongPress');
        }, 500); // 500ms for long press detection
    });
    element.addEventListener('mouseup', function () {
        clearTimeout(timer); // Cancel long press on mouse up
    });
    element.addEventListener('mouseleave', function () {
        clearTimeout(timer); // Cancel long press if mouse leaves the element
    });
}