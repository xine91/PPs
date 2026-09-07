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

const uiBereichStorageKey = 'pulse_ui_bereich';
const appBasePath = '/pulse';

const normalizeBereich = value => (value || '').trim().toLowerCase();

const getBereichSelect = () => document.getElementById('bereichSelect');

const getBereichOption = (bereichSelect, bereichCode) => {
    if (!(bereichSelect instanceof HTMLSelectElement)) {
        return null;
    }

    return Array.from(bereichSelect.options)
        .find(option => normalizeBereich(option.value) === normalizeBereich(bereichCode)) ?? null;
};

const getAvailableBereiche = () => {
    const bereichSelect = getBereichSelect();
    if (!(bereichSelect instanceof HTMLSelectElement)) {
        return [];
    }

    return Array.from(bereichSelect.options)
        .map(option => option.value)
        .filter(value => value && value.trim().length > 0);
};

const getDefaultBereich = availableBereiche => availableBereiche[0] ?? '';

const findBereich = (availableBereiche, candidate) => {
    const normalizedCandidate = normalizeBereich(candidate);
    if (!normalizedCandidate) {
        return null;
    }

    return availableBereiche.find(bereich => normalizeBereich(bereich) === normalizedCandidate) ?? null;
};

const getStoredUiBereich = availableBereiche => {
    const bereich = window.localStorage.getItem(uiBereichStorageKey);
    return findBereich(availableBereiche, bereich) ?? getDefaultBereich(availableBereiche);
};

const getBereichFromPath = availableBereiche => {
    const pathSegments = window.location.pathname.split('/').filter(Boolean);
    if (pathSegments.length === 0) {
        return null;
    }

    const firstSegment = pathSegments[0];
    const bereichSegment = normalizeBereich(firstSegment) === 'pulse' ? pathSegments[1] : firstSegment;
    if (!bereichSegment) {
        return null;
    }

    return findBereich(availableBereiche, bereichSegment);
};

const buildPathWithBereich = (bereich, availableBereiche) => {
    const pathSegments = window.location.pathname.split('/').filter(Boolean);
    if (pathSegments.length > 0 && normalizeBereich(pathSegments[0]) === 'pulse') {
        pathSegments.shift();
    }

    if (pathSegments.length > 0 && findBereich(availableBereiche, pathSegments[0])) {
        pathSegments.shift();
    }

    const pathWithoutBereich = pathSegments.length > 0 ? `/${pathSegments.join('/')}` : '';
    const normalizedPath = pathWithoutBereich === '' ? '/Home/Dashboard' : pathWithoutBereich;
    return `${appBasePath}/${bereich}${normalizedPath}`;
};

const applyUiBereich = (bereich, availableBereiche) => {
    const body = document.body;
    if (!body) {
        return;
    }

    const normalizedBereich = findBereich(availableBereiche, bereich) ?? getDefaultBereich(availableBereiche);

    const bereichClassesToRemove = Array.from(body.classList).filter(cls => cls.startsWith('bereich-'));
    bereichClassesToRemove.forEach(cls => body.classList.remove(cls));

    if (normalizedBereich) {
        const cssBereichClass = `bereich-${normalizeBereich(normalizedBereich)}`;
        body.classList.add(cssBereichClass);
    }

    document.querySelectorAll('[data-nav-bereich]').forEach(section => {
        const sectionBereich = section.getAttribute('data-nav-bereich');
        section.hidden = normalizeBereich(sectionBereich) !== normalizeBereich(normalizedBereich);
    });

    const bereichSelect = getBereichSelect();
    if (bereichSelect instanceof HTMLSelectElement) {
        bereichSelect.value = normalizedBereich;
    }
};

const initializeUiBereichSwitcher = () => {
    const bereichSelect = getBereichSelect();
    if (!(bereichSelect instanceof HTMLSelectElement)) {
        return;
    }

    const availableBereiche = getAvailableBereiche();
    if (availableBereiche.length === 0) {
        return;
    }

    const bereichFromPath = getBereichFromPath(availableBereiche);
    const initialBereich = bereichFromPath ?? getStoredUiBereich(availableBereiche);
    applyUiBereich(initialBereich, availableBereiche);

    bereichSelect.addEventListener('change', () => {
        const selectedBereich = findBereich(availableBereiche, bereichSelect.value) ?? getDefaultBereich(availableBereiche);
        window.localStorage.setItem(uiBereichStorageKey, selectedBereich);
        applyUiBereich(selectedBereich, availableBereiche);

        const targetPath = `${appBasePath}/${selectedBereich}/Home/Dashboard`;
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
    initializeUiBereichSwitcher();
    initializeSidebarCollapseState();
});
