// Sidebar Hover Animation - Sidebar wird beim Hovern erweitert
(function () {
    function init() {
        const sidebar = document.querySelector('.app-sidebar');
        if (!sidebar) return;

        const header = document.querySelector('.app-header');
        const appWrapper = document.querySelector('.app-wrapper');

        // Sidebar standardmaessig collapsed
        sidebar.classList.add('collapsed');

        // Unsichtbarer Hover-Buffer am rechten Rand der collapsed Sidebar
        // erhoeht die Trefferquote und verhindert Flackern beim Ueberlauf.
        const buffer = document.createElement('div');
        buffer.className = 'sidebar-hover-buffer';
        buffer.style.cssText = 'position:absolute;top:0;right:-12px;width:12px;height:100%;pointer-events:auto;';
        sidebar.appendChild(buffer);

        let hoverTimer = null;

        function expand() {
            if (hoverTimer) { clearTimeout(hoverTimer); hoverTimer = null; }
            sidebar.classList.remove('collapsed');
        }

        function collapse() {
            if (hoverTimer) clearTimeout(hoverTimer);
            hoverTimer = setTimeout(function () {
                sidebar.classList.add('collapsed');
                hoverTimer = null;
            }, 120);
        }

        sidebar.addEventListener('mouseenter', expand);
        sidebar.addEventListener('mouseleave', collapse);
        buffer.addEventListener('mouseenter', expand);

        [header, appWrapper].forEach(function (el) {
            if (!el) return;
            el.addEventListener('mouseenter', collapse);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
