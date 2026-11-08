// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("ra-sidebar");
    const menuButtons = document.querySelectorAll(".ra-topbar-menu");
    const closeButtons = document.querySelectorAll("[data-sidebar-close]");
    const setSidebarOpen = (open) => {
        document.body.classList.toggle("ra-nav-open", open);
        menuButtons.forEach(button => button.setAttribute("aria-expanded", String(open)));
    };
    menuButtons.forEach(button => button.addEventListener("click", () => setSidebarOpen(true)));
    closeButtons.forEach(button => button.addEventListener("click", () => setSidebarOpen(false)));
    sidebar?.querySelectorAll("a").forEach(link => link.addEventListener("click", () => setSidebarOpen(false)));
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") setSidebarOpen(false);
    });
    window.addEventListener("resize", () => {
        if (window.innerWidth > 900) setSidebarOpen(false);
    });
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


    const publicMenu = document.querySelector(".ra-public-menu-toggle");
    const publicNav = document.getElementById("ra-public-nav");
    if (publicMenu && publicNav) {
        publicMenu.addEventListener("click", function () {
            const isOpen = document.body.classList.toggle("ra-public-nav-open");
            publicMenu.setAttribute("aria-expanded", String(isOpen));
            publicMenu.setAttribute("aria-label", isOpen ? "Close website navigation" : "Open website navigation");
            publicMenu.textContent = isOpen ? "×" : "☰";
        });
        publicNav.querySelectorAll("a").forEach(function (link) {
            link.addEventListener("click", function () {
                document.body.classList.remove("ra-public-nav-open");
                publicMenu.setAttribute("aria-expanded", "false");
                publicMenu.setAttribute("aria-label", "Open website navigation");
                publicMenu.textContent = "☰";
            });
        });
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                document.body.classList.remove("ra-public-nav-open");
                publicMenu.setAttribute("aria-expanded", "false");
                publicMenu.textContent = "☰";
            }
        });
    }
