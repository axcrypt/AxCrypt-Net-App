function toggleClass() {
    var container = document.getElementById("container");
    var img = document.getElementById("rotateImg");
    container.classList.toggle("hidden");
    img.classList.toggle("rotate");
}

function setupPopupHandlers() {
    const popupContainer = document.getElementById("password-key-share-icon");
    const popupContent = document.querySelector("sharedwith-box");

    popupContainer.onmouseenter =
        function () {
            popupContent.style.display = 'block';
        };
    popupContainer.onmouseleave =
        function () {
            popupContent.style.display = 'none';
        };
}

function confirmDelete() {
    alert('Are you sure you want to delete this password?');
};

function hideElementById() {
    var contain = document.getElementById("share-max-count-error-div");
    contain.style.display = "block"; // Hide the element
}

document.addEventListener('click', CloseContextMenu);

function CloseContextMenu(event) {
    var contextMenu = document.getElementById("contextMenu");
    if (contextMenu !== null) {
        if (event.target !== contextMenu || !contextMenu.contains(event.target)) {
            contextMenu.style.display = "block";
        } else {
            document.getElementById("contextMenu").style.display = "none";
        }
    }
}

window.updateActionButton = (buttonId, content) => {
    const button = document.getElementById(buttonId);
    if (button) {
        button.innerHTML = content;
    }
};

function showRecentFiles() {
    document.getElementById('showclickRecentFiles').style.display = 'block';
    document.getElementById('showclickAll').style.display = 'none';
    document.getElementById('showclickSharedWithMe').style.display = 'none';
}

function showAll() {
    document.getElementById('showAll').style.display = 'block';
    document.getElementById('showSharedWithMe').style.display = 'none';
    document.getElementById('showRecentFiles').style.display = 'none';
}

function showSharedWithMe() {
    document.getElementById('showSharedWithMe').style.display = 'block';
    document.getElementById('showRecentFiles').style.display = 'none';
    document.getElementById('showAll').style.display = 'none';
}

//document.addEventListener("DOMContentLoaded", function () {
//    const recentTab = document.getElementById('recent-tab');
//    const allTab = document.getElementById('all-tab');
//    const sharedTab = document.getElementById('shared-tab');
//    const listItems = document.querySelectorAll('.list-box');

//    function showAllItems() {
//        listItems.forEach(item => {
//            item.style.display = 'flex';
//        });
//    }

//    function hideSharedIcons() {
//        listItems.forEach(item => {
//            if (item.querySelector('.rcnt-fld-bx-pswrd').classList.contains('password-key-share-icons')) {
//                item.style.display = 'none';
//            } else {
//                item.style.display = 'flex';
//            }
//        });
//    }

//    recentTab.addEventListener('click', function (event) {
//        event.preventDefault();
//        showAllItems();
//    });

//    allTab.addEventListener('click', function (event) {
//        event.preventDefault();
//        showAllItems();
//    });

//    sharedTab.addEventListener('click', function (event) {
//        event.preventDefault();
//        hideSharedIcons();
//    });
//});

function defaultPage() {
    document.getElementById('showdefaultPage').style.display = 'block';
    document.getElementById('recentshowNotes').style.display = 'none';
    document.getElementById('recentshowCards').style.display = 'none';
    document.getElementById('recentshowPasswords').style.display = 'none';
}

function showPasswords() {
    var passwordsElement = document.getElementById('recentshowPasswords');
    var hasPasswords = passwordsElement !== null ? passwordsElement.innerHTML.trim().length > 0 : false;
    if (hasPasswords) {
        passwordsElement.style.display = 'block';
        document.getElementById('showdefaultPage').style.display = 'none';
        document.getElementById('recentshowNotes').style.display = 'none';
        document.getElementById('recentshowCards').style.display = 'none';
    }
    else {
        defaultPage();
    }
}

function showNotes() {
    document.getElementById('recentshowNotes').style.display = 'block';
    document.getElementById('showdefaultPage').style.display = 'none';
    document.getElementById('recentshowCards').style.display = 'none';
    document.getElementById('recentshowPasswords').style.display = 'none';
}

function showCards() {
    document.getElementById('recentshowCards').style.display = 'block';
    document.getElementById('recentshowNotes').style.display = 'none';
    document.getElementById('recentshowPasswords').style.display = 'none';
    document.getElementById('showdefaultPage').style.display = 'none';
}