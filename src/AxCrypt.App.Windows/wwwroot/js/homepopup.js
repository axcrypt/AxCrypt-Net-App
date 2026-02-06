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

            if (!contextMenu) return;
            contextMenu.style.visibility = "visible";
            contextMenu.style.display = 'block';

            let contextMenuWidth = contextMenu.offsetWidth;
            let contextMenuHeight = contextMenu.offsetHeight;

            let windowWidth = window.innerWidth;
            let windowHeight = window.innerHeight;
            let padding = 5;

            let posX = e.clientX;
            let posY = e.clientY;

            if (posX + contextMenuWidth + padding > windowWidth) {
                posX = windowWidth - contextMenuWidth - padding;
            }
            if (posX < padding) posX = padding;

            if (posY + contextMenuHeight + padding > windowHeight) {
                posY = windowHeight - contextMenuHeight - padding;
            }
            if (posY < padding) posY = padding;

            contextMenu.style.left = posX + "px";
            contextMenu.style.top = posY + "px";

        }
    }

    document.addEventListener('contextmenu', function (e) {
        if (e.target.closest('tr.context-menu-target')) {
            handleContextMenu(e, 'homeContextMenu', 'tr.context-menu-target');
        } else if (e.target.closest('.recent-folder-action-cls')) {
            handleContextMenu(e, 'securedContextMenu', '.recent-folder-action-cls');
        }
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

    window.registerOutsideClick = (inputEl, popupId, dotNetRef) => {
        const handler = (e) => {
            var popupEl = document.getElementById(popupId);
            if (!inputEl.contains(e.target) && popupEl != undefined && !popupEl.contains(e.target)) {
                dotNetRef.invokeMethodAsync('HideSuggestions');//invoke c# method
                document.removeEventListener('click', handler);
            }
        };
        document.addEventListener('click', handler);
    };


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
        if (targetobj == null) {
            return;
        }
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
            setPositionPopupAtButton("settings-dropdown-click-action", "settings-dropdown-popup", e);
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("settings-dropdown-popup", targetobj)) {
                if (!IgnoreClosePopupOnPopupActions("inactivity-signout-side-popup", targetobj)) {
                    if (!IgnoreClosePopupOnPopupActions("encryption-file-property-side-popup", targetobj)) {
                        if (!IgnoreClosePopupOnPopupActions("advanced-options-side-popup", targetobj)) {
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
        }

        //Handle show/hide the notification menu dropdown popup
        if (targetobj.id === "notification-dropdown-click-action" || targetobj.parentElement.id === "notification-dropdown-click-action") {
            document.getElementById("notification-dropdown-click-action").classList.toggle("active");
            ShowHidePopup("notification-dropdown-popup");
            setPositionPopupAtButton("notification-dropdown-click-action", "notification-dropdown-popup", e);
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

        //Handle show/hide the advanced option menu side popup
        if (targetobj.id === "advanced-options-button-action" || targetobj.parentElement.id === "advanced-options-button-action") {
            ShowHidePopup("advanced-options-side-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("advanced-options-side-popup", targetobj)) {
                HidePopup("advanced-options-side-popup");
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

        //Handle show/hide the advanced filter popup
        if (targetobj.id === "advanced-filter-popup-button-action" || targetobj.parentElement.id === "advanced-filter-popup-button-action") {
            ShowHidePopup("advanced-filter-popup");
        }
        else {
            if (!IgnoreClosePopupOnPopupActions("advanced-filter-popup", targetobj) || targetobj.id === "apply-search-filter") {
                HidePopup("advanced-filter-popup");
            }
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

function setPositionPopupAtButton(buttonId, popupId, e) {
    var button = document.getElementById(buttonId);
    var popup = document.getElementById(popupId);

    if (!button || !popup) return;

    if (popup.style.display != 'block') {
        return;  // Exit the function if popup is already visible
    }

    var clickX = e.clientX;

    // Current window width
    var windowWidth = window.innerWidth;
    var rightMargin = 40;
    // Distance from clicked position to right edge
    var distanceToRight = windowWidth - clickX - rightMargin;

    popup.style.setProperty('--popup-menu-right', distanceToRight + 'px');
}