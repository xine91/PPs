// Sidebar Hover Animation - Sidebar wird beim Hovern erweitert
(function () {
    const sidebar = document.querySelector('.app-sidebar');
    const header = document.querySelector('.app-header');
    const appWrapper = document.querySelector('.app-wrapper');

    if (!sidebar) return;

    // Sidebar wird standardmäßig collapsed
    sidebar.classList.add('collapsed');

    // Hover-Event: Sidebar erweitern
    sidebar.addEventListener('mouseenter', function () {
        sidebar.classList.remove('collapsed');
    });

    // Leave-Event: Sidebar einklappen
    sidebar.addEventListener('mouseleave', function () {
        sidebar.classList.add('collapsed');
    });
})();
