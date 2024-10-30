window.touchEvents = {
    addTouchEventHandlers: function (element, dotNetHelper, fileName) {
        let timer;
        let isLongPress = false;
        element.addEventListener('touchstart', function () {
            isLongPress = false;
            timer = setTimeout(function () {
                isLongPress = true;
                dotNetHelper.invokeMethodAsync('EnableMultiSelect', fileName);
            }, 1000); // 1 second for long press
        });
        element.addEventListener('touchend', function () {
            clearTimeout(timer);
            if (!isLongPress) {
                // Handle short touch if needed
            }
        });
    }
};