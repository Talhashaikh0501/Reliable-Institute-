// Reliable Academy workspace navigation and shared page interactions.
document.addEventListener("DOMContentLoaded", function () {
    const body = document.body;
    const sidebar = document.getElementById("ra-workspace-sidebar");
    const menuButton = document.querySelector("[data-workspace-menu]");
    const closeButtons = document.querySelectorAll("[data-workspace-close]");
    const mobileNavigation = window.matchMedia("(max-width: 900px)");

    if (sidebar && menuButton) {
        const focusableInSidebar = () => Array.from(
            sidebar.querySelectorAll('a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])')
        ).filter(element => !element.hasAttribute("hidden"));

        const setNavigationOpen = (requestedOpen, restoreFocus) => {
            const wasOpen = body.classList.contains("ra-workspace-nav-open");
            const shouldOpen = mobileNavigation.matches && requestedOpen;

            body.classList.toggle("ra-workspace-nav-open", shouldOpen);
            menuButton.setAttribute("aria-expanded", String(shouldOpen));
            menuButton.setAttribute("aria-label", shouldOpen ? "Close navigation" : "Open navigation");
            sidebar.setAttribute("aria-hidden", String(mobileNavigation.matches && !shouldOpen));
            sidebar.inert = mobileNavigation.matches && !shouldOpen;

            if (shouldOpen) {
                const closeButton = sidebar.querySelector(".ra-workspace-close");
                (closeButton || focusableInSidebar()[0])?.focus({ preventScroll: true });
            } else if (restoreFocus && wasOpen) {
                menuButton.focus({ preventScroll: true });
            }
        };

        menuButton.addEventListener("click", function () {
            setNavigationOpen(true, false);
        });

        closeButtons.forEach(button => {
            button.addEventListener("click", function () {
                setNavigationOpen(false, true);
            });
        });

        sidebar.querySelectorAll("a").forEach(link => {
            link.addEventListener("click", function () {
                if (mobileNavigation.matches) setNavigationOpen(false, false);
            });
        });

        document.addEventListener("keydown", function (event) {
            if (!body.classList.contains("ra-workspace-nav-open")) return;

            if (event.key === "Escape") {
                event.preventDefault();
                setNavigationOpen(false, true);
                return;
            }

            if (event.key === "Tab") {
                const focusable = focusableInSidebar();
                if (focusable.length === 0) return;

                const first = focusable[0];
                const last = focusable[focusable.length - 1];

                if (event.shiftKey && document.activeElement === first) {
                    event.preventDefault();
                    last.focus();
                } else if (!event.shiftKey && document.activeElement === last) {
                    event.preventDefault();
                    first.focus();
                }
            }
        });

        const handleBreakpointChange = () => setNavigationOpen(false, false);
        if (typeof mobileNavigation.addEventListener === "function") {
            mobileNavigation.addEventListener("change", handleBreakpointChange);
        } else {
            mobileNavigation.addListener(handleBreakpointChange);
        }
        setNavigationOpen(false, false);
    }

    const home = document.querySelector(".rci-home");
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (home && "IntersectionObserver" in window && !reduceMotion) {
        const revealTargets = home.querySelectorAll(
            [
                // Original homepage selectors retained for compatibility.
                ".rci-intro-grid",
                ".rci-programme-heading",
                ".rci-programme-card",
                ".rci-approach-heading",
                ".rci-principles article",
                ".rci-history > div",
                ".rci-visit-copy",
                ".rci-visit-art",

                // New photo-led homepage: reveal each meaningful piece as it enters view.
                ".rci-next-proof-intro",
                ".rci-next-proof-item",
                ".rci-next-intro-copy > h2",
                ".rci-next-intro-copy > p",
                ".rci-next-intro-copy > a",
                ".rci-next-intro-visual",
                ".rci-next-programmes-inner > .rci-next-section-kicker",
                ".rci-next-programmes-heading",
                ".rci-next-programme-card",
                ".rci-next-programmes-foot",
                ".rci-next-method > .rci-next-section-kicker",
                ".rci-next-method-photo",
                ".rci-next-method-copy > h2",
                ".rci-next-method-copy > p",
                ".rci-next-method-steps article",
                ".rci-next-results-copy > *",
                ".rci-next-topper-wall-head",
                ".rci-next-topper-card",
                ".rci-next-topper-foot",
                ".rci-next-gallery-heading",
                ".rci-next-gallery-grid figure",
                ".rci-next-stock-note",
                ".rci-next-visit-copy > *",
                ".rci-next-address-card",
                ".rci-next-footer-main > *",
                ".rci-next-footer-bottom",
                ".rci-next-credits"
            ].join(", ")
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
    }

    const publicMenu = document.querySelector(".ra-public-menu-toggle");
    const publicNav = document.getElementById("ra-public-nav");
    const closePublicMenu = () => {
        body.classList.remove("ra-public-nav-open");
        if (!publicMenu) return;
        publicMenu.setAttribute("aria-expanded", "false");
        publicMenu.setAttribute("aria-label", "Open website navigation");
        publicMenu.textContent = "☰";
    };

    if (publicMenu && publicNav) {
        publicMenu.addEventListener("click", function () {
            const isOpen = !body.classList.contains("ra-public-nav-open");
            body.classList.toggle("ra-public-nav-open", isOpen);
            publicMenu.setAttribute("aria-expanded", String(isOpen));
            publicMenu.setAttribute("aria-label", isOpen ? "Close website navigation" : "Open website navigation");
            publicMenu.textContent = isOpen ? "×" : "☰";
        });

        publicNav.querySelectorAll("a").forEach(link => link.addEventListener("click", closePublicMenu));
        document.addEventListener("click", event => {
            if (!publicNav.contains(event.target) && !publicMenu.contains(event.target)) closePublicMenu();
        });
        document.addEventListener("keydown", event => {
            if (event.key === "Escape") closePublicMenu();
        });
        window.addEventListener("resize", () => {
            if (window.innerWidth > 820) closePublicMenu();
        });
    }

    document.querySelectorAll("[data-password-toggle]").forEach(function (button) {
        button.addEventListener("click", function () {
            const input = document.getElementById(button.getAttribute("data-password-toggle"));
            if (!input) return;
            const reveal = input.type === "password";
            input.type = reveal ? "text" : "password";
            button.textContent = reveal ? "Hide" : "Show";
            button.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
            button.setAttribute("aria-pressed", String(reveal));
        });
    });
});
