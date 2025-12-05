let helpSlideIndex = 0;
let totalHelpSlides = 7;

// Mask
function addMask() {
    const mask = document.createElement("div");
    mask.className = "help-overlay-mask";
    document.body.appendChild(mask);
}

// Clear slide
function clearHelp() {
    document.querySelectorAll(
        ".help-overlay-mask, .help-tooltip, .help-buttons, .help-pagination-dots, .help-arrow-img"
    ).forEach(x => x.remove());
}

// Tooltip
function createTooltip(rect, text, offsetTop = 8, offsetLeft = -30, extraClass = "", customWidth = null) {
    const tip = document.createElement("div");
    tip.className = "help-tooltip " + extraClass;
    tip.innerHTML = text;

    if (customWidth) {
        tip.style.width = customWidth + "px";
    }

    tip.style.top = rect.bottom + offsetTop + 55 + "px";
    tip.style.left = (rect.left + offsetLeft + 10) + "px";
    document.body.appendChild(tip);
}

// Buttons
function createButtons(nextAction, skipAction) {
    const btnContainer = document.createElement("div");
    btnContainer.className = "help-buttons";

    const nextBtn = document.createElement("button");
    nextBtn.className = "help-btn help-btn-next";
    nextBtn.innerText = "Got it";
    nextBtn.onclick = nextAction;
    btnContainer.appendChild(nextBtn);

    const skipBtn = document.createElement("button");
    skipBtn.className = "help-btn help-btn-skip";
    skipBtn.innerText = "Skip";
    skipBtn.onclick = skipAction;
    btnContainer.appendChild(skipBtn);

    document.body.appendChild(btnContainer);
}

// Generic slide builder
function buildSlide(items, nextSlide = null, extraTooltipClass = "", offsets = { top: 8, left: -30 }, rotateArrows = false, arrowTopOffset = 0, arrowLeftOffset = 0) {
    addMask();

    const arrowImages = [
        "images/help-arrow1.svg",
        "images/help-arrow2.svg",
        "images/help-arrow3.svg",
        "images/help-arrow4.svg",
        "images/help-arrow5.svg"
    ];

    items.forEach((i, index) => {
        const el = document.getElementById(i.id);
        if (!el) return;

        const rect = el.getBoundingClientRect();

        createTooltip(rect, i.text, offsets.top, offsets.left, extraTooltipClass, i.width || null);

        const imgUrl = arrowImages[index % arrowImages.length];

        createArrow(
            rect,
            offsets.top,
            offsets.left,
            imgUrl,
            rotateArrows,
            arrowTopOffset,
            arrowLeftOffset
        );
    });

    createPaginationDots();
    createButtons(
        () => { clearHelp(); if (nextSlide) nextSlide(); },
        () => clearHelp()
    );
}

function createArrow(rect, offsetTop, offsetLeft, imgUrl, rotate = false, arrowTopOffset = 0, arrowLeftOffset = 0) {
    const img = document.createElement("img");
    img.className = "help-arrow-img";
    img.src = imgUrl;

    img.style.position = "absolute";

    // only for SideMenu + About slides
    img.style.top = (rect.bottom + offsetTop + arrowTopOffset) + "px";
    img.style.left = (rect.left + offsetLeft + 40 + arrowLeftOffset) + "px";

    img.style.height = "60px";
    img.style.zIndex = "999999";

    if (rotate) {
        img.style.transform = "rotate(270deg)";
    }

    document.body.appendChild(img);
}

function createPaginationDots() {
    const dotContainer = document.createElement("div");
    dotContainer.className = "help-pagination-dots";

    for (let i = 0; i < totalHelpSlides; i++) {
        const dot = document.createElement("div");
        dot.className = "help-dot" + (i === helpSlideIndex ? " active" : "");
        dotContainer.appendChild(dot);
    }

    document.body.appendChild(dotContainer);
}

function startHomeHelp() {
    helpSlideIndex = 0;
    updateZIndexForSlide(helpSlideIndex);
    const items = [
        { id: "help-open", text: "Use <b>Open Secured</b> to choose an encrypted file you want to open." },
        { id: "help-secure", text: "Click <b>Secure</b> to choose a file you want to encrypt." },
        { id: "help-stop", text: "Use <b>Stop Securing</b> to decrypt an encrypted file back to original." },
        { id: "help-sharekey", text: "Use <b>Share Keys</b> to share file access with colleagues and friends." },
        { id: "help-clean", text: "The <b>Broom</b> icon shows up when something needs <b>clean-up</b>, like files that weren’t updated correctly or unencrypted files in monitored folders. Clicking the <b>Broom</b> icon will encrypt them.", width: 250 }
    ];
    buildSlide(items, showCloudServicesHelp);
}

function showCloudServicesHelp() {
    helpSlideIndex = 1;
    updateZIndexForSlide(helpSlideIndex);
    const parent = document.getElementById("help-cloud-services");
    if (!parent) return;

    const boxes = parent.querySelectorAll(".assets-box");
    if (boxes.length === 0) return;

    addMask();

    boxes.forEach(box => {
        const rect = box.getBoundingClientRect();
    });

    const first = boxes[0].getBoundingClientRect();
    const last = boxes[boxes.length - 1].getBoundingClientRect();

    const combinedRect = {
        top: first.top,
        left: first.left,
        bottom: last.bottom,
        right: last.right
    };

    createTooltip(
        combinedRect,
        "Use <b>Cloud Services</b> to manage encrypted files on your installed cloud drives.",
        10,
        0
    );

    createArrow(
        {
            bottom: (combinedRect.top + combinedRect.bottom) / 2,
            left: (combinedRect.left + combinedRect.right) / 2
        },
        10 + 15,
        0 - 70,
        "images/help-arrow4.svg"
    );
    createPaginationDots();
    createButtons(
        () => { clearHelp(); startTopMenuHelp(); },
        () => clearHelp()
    );
}

function startTopMenuHelp() {
    helpSlideIndex = 2;
    updateZIndexForSlide(helpSlideIndex);

    const items = [
        { id: "help-language", text: "Select your preferred <b>Language</b>." },
        { id: "help-settings", text: "Use <b>Settings</b> to configure options like inactivity sign-out, restoring original file names, offline mode, and advanced encryption.", width: 150 },
        { id: "help-profile", text: "Use <b>Account</b> to access and manage your account, subscription, keys, and sign out." }
    ];
    buildSlide(items, startMoreActionHelp);
}

function startMoreActionHelp() {
    helpSlideIndex = 3;
    updateZIndexForSlide(helpSlideIndex);

    const items = [
        { id: "help-anymous-rename", text: "Click <b>Anonymous Rename</b> to hide the real name of an encrypted file by renaming it with a random name.", width: 200 },
        { id: "help-secure-dlt", text: "Click <b>Secure Delete</b> to safely remove your files.", width: 200 },
        { id: "help-upgrade-files", text: "Click <b>Upgrade Files</b> to choose a folder of AES-128 encrypted files and upgrade their encryption to AES-256.", width: 200 }
    ];
    buildSlide(items, showRecentFilesHelp);
}

function showRecentFilesHelp() {
    if (!document.getElementById("help-recent-file")) {
        return showSideMenuHelp();
    }

    helpSlideIndex = 4;
    updateZIndexForSlide(helpSlideIndex);

    const items = [
        {
            id: "help-recent-file",
            text: "The <b>Recent Files</b> section lets you manage your latest encrypted files and view information like file name, size, encryption algorithm, date, and who has access."
        }
    ];

    buildSlide(items, showSideMenuHelp, "help-width");
}

function showSideMenuHelp() {
    helpSlideIndex = 5;
    updateZIndexForSlide(helpSlideIndex);

    const items = [
        { id: "help-home", text: "Use <b>Home</b> to access primary AxCrypt features and manage files." },
        { id: "help-secured-folders", text: "Click <b>Secured Folders</b> to add and manage folders." },
        { id: "help-pswd-mngr", text: "Use <b>Password Manager</b> to secure & manage passwords, notes and card." },
        { id: "help-scrd-mssngr", text: "Use <b>Secured Messenger</b> to send secured messages." },
        { id: "help-txt-encyptn", text: "Click <b>Text Encryption</b> to encrypt your text." },
        { id: "help-nofify", text: "Click <b>Notifications</b> to view your notifications" },
        { id: "help-support", text: "Click <b>Support</b> to get help." }
    ];

    buildSlide(items, showAboutMenuHelp, "sidemenu-width", { top: -100, left: 440 }, true, 40, -80);
}

function showAboutMenuHelp() {
    helpSlideIndex = 6;
    updateZIndexForSlide(helpSlideIndex);

    const items = [
        { id: "help-feedback", text: "You can share your ideas,questions and <b>Feedback</b> using a form." },
        { id: "help-about", text: "Know more <b>About AxCrypt.</b>" }
    ];

    buildSlide(items, null, "abt-width", { top: -100, left: 260 }, true, 40, -80);
}

function updateZIndexForSlide(slideIndex) {
    const slideMap = {
        0: ".act-box",
        1: ".assets-box",
        2: ".dropdown, .sttng-icon, .mb-prfle",
        3: ".mre-actn-box",
        4: ".help-recent-files",
        5: ".flex-column",
        6: ".abt-sec"
    };

    document.querySelectorAll(".z-top").forEach(x => x.classList.remove("z-top"));

    const selector = slideMap[slideIndex];
    if (!selector) return;

    document.querySelectorAll(selector).forEach(x => {
        x.classList.add("z-top");
    });
}