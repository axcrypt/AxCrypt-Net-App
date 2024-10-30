function toggleSection(mySection, toggleImageClosed, toggleImageOpen) {
    var section = document.getElementById(mySection);
    var toggleImageClosed = document.getElementById("toggleImageClosed");
    var toggleImageOpen = document.getElementById("toggleImageOpen");

    if (section.style.display === "none" || section.style.display === "") {
        section.style.display = "flex";
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
        handleContextMenu(e, 'securedContextMenu', '.rcnt-fld-bx');
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
    });
});