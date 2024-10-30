window.addSwipeHandlersToNotifications = () => {
    document.querySelectorAll('.notification-item').forEach((element) => {
        window.addSwipeLeftHandler(element.id);
    });
}

window.addSwipeLeftHandler = (elementId) => {
    const element = document.getElementById(elementId);
    let xDown = null;
    let yDown = null;
    let touchStartTime = null;

    element.addEventListener('touchstart', handleTouchStart, false);
    element.addEventListener('touchmove', handleTouchMove, false);
    element.addEventListener('touchend', handleTouchEnd, false);

    function handleTouchStart(evt) {
        const firstTouch = evt.touches[0];
        xDown = firstTouch.clientX;
        yDown = firstTouch.clientY;
        touchStartTime = new Date().getTime();
    }

    function handleTouchMove(evt) {
        if (!xDown || !yDown) {
            return;
        }
        const xUp = evt.touches[0].clientX;
        const yUp = evt.touches[0].clientY;
        const xDiff = xDown - xUp;
        const yDiff = yDown - yUp;

        if (Math.abs(xDiff) > Math.abs(yDiff)) {
            if (xDiff > 0) {
                element.classList.add('swiped-left');
                element.nextElementSibling.style.display = "block"; // Show delete button
            }
            else {
                element.classList.remove('swiped-left');
                element.nextElementSibling.style.display = "none"; // Hide delete button
            }
        }

        xDown = null;
        yDown = null;
    }

    function handleTouchEnd(evt) {
        const touchEndTime = new Date().getTime();
        const touchDuration = touchEndTime - touchStartTime;

        if (touchDuration < 200) {
            $(element).click(function () {
                $("#view-message").show();
                $(".empty-sec").hide();
                $("#new-message").hide();
                $("#mb-new-message").hide();
            });
        }
    }
};


