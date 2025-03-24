function toggleSection(mySection, toggleImageClosed, toggleImageOpen) {
    var section = document.getElementById(mySection);
    var toggleImageClosed = document.getElementById("toggleImageClosed");
    var toggleImageOpen = document.getElementById("toggleImageOpen");

    if (section.style.display === "none" || section.style.display === "") {
        section.style.display = "grid";
        toggleImageClosed.style.display = "none";
        toggleImageOpen.style.display = "flex";
    }
    else {
        section.style.display = "none";
        toggleImageClosed.style.display = "flex";
        toggleImageOpen.style.display = "none";
    }
}

function mreActPopup() {
    var toggleActFeatClosed = document.getElementById("toggleActFeatClosed");
    var toggleActFeatOpen = document.getElementById("toggleActFeatOpen");
    var mreActFeat = document.getElementById("mreActFeat");
    var mreActSec = document.getElementById("mreActSec");

    if (toggleActFeatClosed.style.display === "none") {
        toggleActFeatClosed.style.display = "flex";
        toggleActFeatOpen.style.display = "none";
        mreActFeat.style.display = "none";
        mreActSec.classList.add("mre-act-dflt");
    } else {
        toggleActFeatClosed.style.display = "none";
        toggleActFeatOpen.style.display = "flex";
        mreActFeat.style.display = "flex";
        mreActSec.classList.add("mre-act-actv");
    }
}

function showRecentFiles() {
    document.getElementById('recentFilesSection').style.display = 'block';
    document.getElementById('recentFoldersSection').style.display = 'none';

    document.getElementById('rct-fle-lnk').classList.add('active-link');
    document.getElementById('recentFoldersLnk').classList.remove('active-link');
}

function showRecentFolders() {
    document.getElementById('recentFilesSection').style.display = 'none';
    document.getElementById('recentFoldersSection').style.display = 'block';

    document.getElementById('rct-fle-lnk').classList.remove('active-link');
    document.getElementById('recentFoldersLnk').classList.add('active-link');
}

function showFltPopup() {
    var popup = document.getElementById("fltrpopup");
    if (popup.style.display === "none" || popup.style.display === "") {
        popup.style.display = "block";

        var radios = document.querySelectorAll("#fltrpopup input[type='radio']");
        radios.forEach(function (radio) {
            radio.addEventListener("change", function () {
                popup.style.display = "none";
            });
        });
    } else {
        popup.style.display = "none";
    }
}

function highlightButton(event, button) { //feedback
    event.preventDefault();
    document.querySelectorAll('.flex-start .button').forEach(btn => {
        btn.classList.remove('highlighted', 'font-medium');
    });
    button.classList.add('highlighted', 'font-medium');
}

// Function to handle context menu
document.addEventListener('DOMContentLoaded', (event) => {
    function handleContextMenu(e, menuId, targetClass) {
        let target = e.target.closest(targetClass);
        if (target) {
            e.preventDefault();
            document.querySelectorAll(targetClass + '.selected').forEach(el => {
                el.classList.remove('selected');
            });
            target.classList.add('selected');
            let contextMenu = document.getElementById(menuId);
            contextMenu.style.display = 'block';
            let rect = target.getBoundingClientRect();
            let distance = 10;
            let maxRightSpace = 100;
            let maxTopSpace = 40;

            let leftPosition = Math.min(rect.right + distance, window.innerWidth - maxRightSpace - contextMenu.offsetWidth);
            let bottomPosition = Math.min(rect.top + distance, window.innerHeight - maxTopSpace - contextMenu.offsetHeight);

            contextMenu.style.left = leftPosition + 'px';
            contextMenu.style.top = bottomPosition + 'px';
        }
    }

    // Event listener for recent files context menu
    document.addEventListener('contextmenu', function (e) {
        handleContextMenu(e, 'homeContextMenu', 'tr.context-menu-target');
    });

    // Event listener for secured folders context menu
    document.addEventListener('contextmenu', function (e) {
        handleContextMenu(e, 'securedContextMenu', '.recent-folder-action-cls');
    });

    document.addEventListener('click', function (e) {
        let homeContextMenu = document.getElementById('homeContextMenu');
        if (homeContextMenu !== null) {
            if (homeContextMenu.style.display === 'block') {
                homeContextMenu.style.display = 'none';
                document.querySelectorAll('tr.context-menu-target.selected').forEach(row => {
                    row.classList.remove('selected');
                });
            }
        }
    });

    document.addEventListener('click', function (e) {
        let securedContextMenu = document.getElementById('securedContextMenu');
        if (securedContextMenu !== null) {
            if (securedContextMenu.style.display === 'block') {
                securedContextMenu.style.display = 'none';
                document.querySelectorAll('.rcnt-fld-bx.selected').forEach(row => {
                    row.classList.remove('selected');
                });
            }
        }

        var targetobj = e.target;
        //Handle show/hide the language dropdown popup
        var langSelArrow = document.getElementById("lang-action-btn-arw");
        if (targetobj.id === "lang-dropdown-action-btn" || targetobj.parentElement.id === "lang-dropdown-action-btn") {
            ShowHidePopup("lang-dropdown-popup");
            langSelArrow.classList.toggle("down");
        }
        else {
            HidePopup("lang-dropdown-popup");
            if (langSelArrow !== undefined) {
                langSelArrow.classList.add("down");
            }
        }

        //Handle show/hide the settings menu dropdown popup
        if (targetobj.id === "settings-dropdown-click-action" || targetobj.parentElement.id === "settings-dropdown-click-action") {
            document.getElementById("settings-dropdown-click-action").classList.toggle("active");
            ShowHidePopup("settings-dropdown-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("settings-dropdown-popup", targetobj)) {
                if (!IgnoreClosePopupOnPopupActions("inactivity-signout-side-popup", targetobj)) {
                    if (!IgnoreClosePopupOnPopupActions("encryption-file-property-side-popup", targetobj)) {
                        if (!IgnoreClosePopupOnPopupActions("debug-side-popup", targetobj)) {
                            if (document.getElementById("settings-dropdown-click-action") != undefined) {
                                document.getElementById("settings-dropdown-click-action").classList.remove("active");
                            }
                            HidePopup("settings-dropdown-popup");
                        }
                    }
                }
            }
        }

        //Handle show/hide the notification menu dropdown popup
        if (targetobj.id === "notification-dropdown-click-action" || targetobj.parentElement.id === "notification-dropdown-click-action") {
            document.getElementById("notification-dropdown-click-action").classList.toggle("active");
            ShowHidePopup("notification-dropdown-popup");
        }
        else {
            if (document.getElementById("notification-dropdown-click-action") != undefined) {
                document.getElementById("notification-dropdown-click-action").classList.remove("active");
            }
            HidePopup("notification-dropdown-popup");
        }

        //Handle show/hide the inactivity sign out menu side popup
        if (targetobj.id === "inactivity-signout-button-action" || targetobj.parentElement.id === "inactivity-signout-button-action") {
            ShowHidePopup("inactivity-signout-side-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("inactivity-signout-side-popup", targetobj)) {
                HidePopup("inactivity-signout-side-popup");
            }
        }

        //Handle show/hide the encryption file property menu side popup
        if (targetobj.id === "encryption-file-property-button-action" || targetobj.parentElement.id === "encryption-file-property-button-action") {
            ShowHidePopup("encryption-file-property-side-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("encryption-file-property-side-popup", targetobj)) {
                HidePopup("encryption-file-property-side-popup");
            }
        }

        //Handle show/hide the debug menu side popup
        if (targetobj.id === "debug-button-action" || targetobj.parentElement.id === "debug-button-action") {
            ShowHidePopup("debug-side-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("debug-side-popup", targetobj)) {
                HidePopup("debug-side-popup");
            }
        }

        //Handle show/hide the accounts menu dropdown popup
        if (targetobj.id === "accounts-dropdown-click-action" || targetobj.parentElement.id === "accounts-dropdown-click-action") {
            ShowHidePopup("accounts-dropdown-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("accounts-dropdown-popup", targetobj)) {
                if (!IgnoreClosePopupOnPopupActions("subscription-details-side-popup", targetobj)) {
                    if (!IgnoreClosePopupOnPopupActions("key-management-popup", targetobj)) {
                        HidePopup("accounts-dropdown-popup");
                    }
                }
            }
        }

        //Handle show/hide the key management popup
        if (targetobj.id === "key-management-button-action" || targetobj.parentElement.id === "key-management-button-action") {
            ShowHidePopup("key-management-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("key-management-popup", targetobj)) {
                HidePopup("key-management-popup");
            }
        }
     
        //Handle show/hide the subscriptiondetails menu side popup
        if (targetobj.id === "subscription-details-button-action" || targetobj.parentElement.id === "subscription-details-button-action") {
            ShowHidePopup("subscription-details-side-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("subscription-details-side-popup", targetobj)) {
                HidePopup("subscription-details-side-popup");
            }
        }

        if (targetobj.id === "close-subscription-details-side-popup") {
            HidePopup("subscription-details-side-popup");
        }
        
    });
});

function ShowHidePopup(popupId) {
    const popup = document.getElementById(popupId);
    if (popup !== undefined) {
        popup.style.display = popup.style.display === 'block' ? 'none' : "block";
    }
}

function HidePopup(popupId) {
    const popup = document.getElementById(popupId);
    if (popup !== undefined && popup !== null) {
        popup.style.display = 'none';
    }
}

function IgnoreClosePopupOnPopupActions(popupId, targetobj) {

    if (targetobj.id === popupId || targetobj.parentElement.id === popupId) {
        return true;
    }

    var offsetParent = targetobj.parentElement.offsetParent;
    if (offsetParent === null) {
        return false;
    }

    if (offsetParent.id === popupId) {
        return true;
    }

    offsetParent = offsetParent.offsetParent;
    if (offsetParent === null) {
        return false;
    }

    if (offsetParent.id === popupId) {
        return true;
    }
}