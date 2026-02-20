window.initDescriptionToggle = (desc) => {
    const btn = desc.nextElementSibling; // assumes button is right after description

    // Temporarily expand to measure full height
    desc.classList.add("expanded");
    const fullHeight = desc.scrollHeight;
    desc.classList.remove("expanded");

    const clampedHeight = desc.clientHeight;

    // Show button only if content exceeds 3 lines
    if (fullHeight > clampedHeight) {
        btn.style.display = "inline-block";
    } else {
        btn.style.display = "none";
    }
};

window.toggleDescription = (desc, expand) => {
    const btn = desc.nextElementSibling;

    if (expand) {
        desc.classList.add("expanded");
        btn.textContent = "View Less";
    } else {
        desc.classList.remove("expanded");
        btn.textContent = "View More";
    }
};