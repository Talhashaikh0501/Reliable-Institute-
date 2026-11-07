// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function () {
    const home = document.querySelector(".rci-home");
    if (!home || !("IntersectionObserver" in window)) return;

    const revealTargets = home.querySelectorAll(
        ".rci-intro-grid, .rci-programme-heading, .rci-programme-card, .rci-approach-heading, .rci-principles article, .rci-history > div, .rci-visit-copy, .rci-visit-art"
    );

    revealTargets.forEach((element, index) => {
        element.classList.add("rci-reveal");
        if (index % 3 === 1) element.classList.add("rci-reveal-delay-1");
        if (index % 3 === 2) element.classList.add("rci-reveal-delay-2");
    });

    home.classList.add("rci-motion-ready");
    const observer = new IntersectionObserver((entries, currentObserver) => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add("rci-visible");
            currentObserver.unobserve(entry.target);
        });
    }, { threshold: 0.12, rootMargin: "0px 0px -35px 0px" });

    revealTargets.forEach(element => observer.observe(element));
});
