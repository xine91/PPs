const initializeNotifications = () => {
    if (!window.Notiflix?.Notify) {
        return;
    }

    window.Notiflix.Notify.init({
        width: '400px',
        fontSize: '14px',
        position: 'right-bottom',
        timeout: 3000,
        messageMaxLength: 200
    });
};

const uiModeStorageKey = 'pulse_ui_mode';
const appBasePath = '/pulse';

const normalizeMode = value => (value || '').trim().toLowerCase();

const getModeSelect = () => document.getElementById('uiModeSelect');

const getModeOption = (modeSelect, modeCode) => {
    if (!(modeSelect instanceof HTMLSelectElement)) {
        return null;
    }

    return Array.from(modeSelect.options)
        .find(option => normalizeMode(option.value) === normalizeMode(modeCode)) ?? null;
};

const getAvailableModes = () => {
    const modeSelect = getModeSelect();
    if (!(modeSelect instanceof HTMLSelectElement)) {
        return [];
    }

    return Array.from(modeSelect.options)
        .map(option => option.value)
        .filter(value => value && value.trim().length > 0);
};

const getDefaultMode = availableModes => availableModes[0] ?? '';

const findMode = (availableModes, candidate) => {
    const normalizedCandidate = normalizeMode(candidate);
    if (!normalizedCandidate) {
        return null;
    }

    return availableModes.find(mode => normalizeMode(mode) === normalizedCandidate) ?? null;
};

const getStoredUiMode = availableModes => {
    const mode = window.localStorage.getItem(uiModeStorageKey);
    return findMode(availableModes, mode) ?? getDefaultMode(availableModes);
};

const getModeFromPath = availableModes => {
    const pathSegments = window.location.pathname.split('/').filter(Boolean);
    if (pathSegments.length === 0) {
        return null;
    }

    const firstSegment = pathSegments[0];
    const modeSegment = normalizeMode(firstSegment) === 'pulse' ? pathSegments[1] : firstSegment;
    if (!modeSegment) {
        return null;
    }

    return findMode(availableModes, modeSegment);
};

const buildPathWithMode = (mode, availableModes) => {
    const pathSegments = window.location.pathname.split('/').filter(Boolean);
    if (pathSegments.length > 0 && normalizeMode(pathSegments[0]) === 'pulse') {
        pathSegments.shift();
    }

    if (pathSegments.length > 0 && findMode(availableModes, pathSegments[0])) {
        pathSegments.shift();
    }

    const pathWithoutMode = pathSegments.length > 0 ? `/${pathSegments.join('/')}` : '';
    const normalizedPath = pathWithoutMode === '' ? '/Home/Dashboard' : pathWithoutMode;
    return `${appBasePath}/${mode}${normalizedPath}`;
};

const applyUiMode = (mode, availableModes) => {
    const body = document.body;
    if (!body) {
        return;
    }

    const normalizedMode = findMode(availableModes, mode) ?? getDefaultMode(availableModes);

    const modeClassesToRemove = Array.from(body.classList).filter(cls => cls.startsWith('mode-'));
    modeClassesToRemove.forEach(cls => body.classList.remove(cls));

    if (normalizedMode) {
        const cssModeClass = `mode-${normalizeMode(normalizedMode)}`;
        body.classList.add(cssModeClass);
    }

    document.querySelectorAll('[data-nav-mode]').forEach(section => {
        const sectionMode = section.getAttribute('data-nav-mode');
        section.hidden = normalizeMode(sectionMode) !== normalizeMode(normalizedMode);
    });

    const modeSelect = getModeSelect();
    if (modeSelect instanceof HTMLSelectElement) {
        modeSelect.value = normalizedMode;
    }
};

const initializeUiModeSwitcher = () => {
    const modeSelect = getModeSelect();
    if (!(modeSelect instanceof HTMLSelectElement)) {
        return;
    }

    const availableModes = getAvailableModes();
    if (availableModes.length === 0) {
        return;
    }

    const modeFromPath = getModeFromPath(availableModes);
    const initialMode = modeFromPath ?? getStoredUiMode(availableModes);
    applyUiMode(initialMode, availableModes);

    modeSelect.addEventListener('change', () => {
        const selectedMode = findMode(availableModes, modeSelect.value) ?? getDefaultMode(availableModes);
        window.localStorage.setItem(uiModeStorageKey, selectedMode);
        applyUiMode(selectedMode, availableModes);

        const selectedOption = getModeOption(modeSelect, selectedMode);
        const homeController = selectedOption?.dataset.homeController || 'Home';
        const homeAction = selectedOption?.dataset.homeAction || 'Dashboard';
        const targetPath = `${appBasePath}/${selectedMode}/${homeController}/${homeAction}`;
        const currentPath = `${window.location.pathname}${window.location.search}${window.location.hash}`;
        const nextPath = `${targetPath}${window.location.search}${window.location.hash}`;

        if (currentPath !== nextPath) {
            window.location.assign(nextPath);
        }
    });
};

const initializeSidebarCollapseState = () => {
    const sections = document.querySelectorAll('.collapse[id][data-collapse-persist], #collapseGeneral');
    sections.forEach(section => {
        if (!(section instanceof HTMLElement)) {
            return;
        }

        const id = section.id;
        if (!id || section.querySelector('.nav-link.active')) {
            return;
        }

        if (window.localStorage.getItem(`sidebar_${id}`) === 'collapsed') {
            section.classList.remove('show');
            const button = document.querySelector(`[data-bs-target="#${id}"]`);
            button?.setAttribute('aria-expanded', 'false');
        }

        section.addEventListener('hide.bs.collapse', () => {
            window.localStorage.setItem(`sidebar_${id}`, 'collapsed');
        });

        section.addEventListener('show.bs.collapse', () => {
            window.localStorage.setItem(`sidebar_${id}`, 'expanded');
        });
    });
};

document.addEventListener('DOMContentLoaded', () => {
    initializeNotifications();
    initializeUiModeSwitcher();
    initializeSidebarCollapseState();
});
