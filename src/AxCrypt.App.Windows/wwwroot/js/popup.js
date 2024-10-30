//function hideModalDialog() {
//    var modal = document.getElementById('upg-popup');
//    modal.style.display = 'none';
//}

function hideSubDialog() {
    var modal = document.getElementById('upg-sub-popup');
    modal.style.display = 'none';
}

function closeSharePopup() {
    var shareKey = document.getElementById("sharekey");
    if (shareKey) {
        shareKey.style.display = "none";
    } else {
        console.error("Element with ID 'sharekey' not found.");
    }
}

//close popup action- common
function closeModalDialog(modalId) {
    var modal = document.getElementById(modalId);
    if (modal) {
        console.log('Hiding modal:', modalId);
        modal.style.display = 'none';
    } else {
        console.log('Modal not found:', modalId);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.close-img').forEach(function (element) {
        element.addEventListener('click', function () {
            var modalElement = element.closest('.modalDialog');
            if (modalElement) {
                var modalId = modalElement.id;
                console.log('Close button clicked. Modal ID:', modalId);
                closeModalDialog(modalId);
            } else {
                console.log('Modal element not found for close button.');
            }
        });
    });
});


//document.addEventListener('click', function (event) {
//    var deskAcntPopup = document.querySelector('.model-popup');
//    var targetElement = event.target; // Clicked element

//    // Check if the clicked element is not inside the desk-acnt-popup
//    if (!deskAcntPopup.contains(targetElement)) {
//        deskAcntPopup.style.display = 'none';
//    }
//});

//hide popup when clicked outside whole project
document.addEventListener('click', function (event) {
    var popups = document.querySelectorAll('.hide-model-popup');

    popups.forEach(function (popup) {
        var targetElement = event.target; // Clicked element

        // Check if the clicked element is not inside the popup
        if (!popup.contains(targetElement)) {
            // The click was outside of the popup, so hide it
            popup.style.display = 'none';
        }
    });
});
