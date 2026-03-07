/**
 * Version Switcher for DocFX Documentation
 * Fetches version manifest and enables version switching
 */

(function() {
  'use strict';

  const VERSION_MANIFEST_URL = '/wt/versions.json';

  /**
   * Fetches the version manifest from the server
   * @returns {Promise<Object>} The version manifest object
   */
  async function fetchVersionManifest() {
    try {
      const response = await fetch(VERSION_MANIFEST_URL);
      if (!response.ok) {
        throw new Error(`Failed to fetch version manifest: ${response.status}`);
      }
      return await response.json();
    } catch (error) {
      console.error('Error fetching version manifest:', error);
      return null;
    }
  }

  /**
   * Extracts the version from the current URL path
   * Expected format: /v{major}.{minor}/...
   * @returns {string|null} The version string (e.g., "v1.0") or null if not found
   */
  function getCurrentVersion() {
    const path = window.location.pathname;
    const versionMatch = path.match(/\/v(\d+\.\d+)\//);
    return versionMatch ? `v${versionMatch[1]}` : null;
  }

  /**
   * Gets the current page path without the version prefix
   * @param {string} currentVersion The current version (e.g., "v1.0")
   * @returns {string} The page path without version
   */
  function getCurrentPagePath(currentVersion) {
    const path = window.location.pathname;
    if (currentVersion) {
      // Remove version prefix: /v1.0/path/to/page.html -> /path/to/page.html
      return path.replace(`/${currentVersion}`, '');
    }
    return path;
  }

  /**
   * Builds the URL for a specific version and page
   * @param {string} version The target version (e.g., "v1.0")
   * @param {string} pagePath The page path without version prefix
   * @returns {string} The full URL for the version and page
   */
  function buildVersionUrl(version, pagePath) {
    // Encode version to prevent XSS
    const safeVersion = encodeURIComponent(version);

    // Ensure pagePath starts with /
    if (!pagePath.startsWith('/')) {
      pagePath = '/' + pagePath;
    }
    // Encode page path components
    const safePagePath = pagePath.split('/').map(segment => encodeURIComponent(segment)).join('/');
    return `/${safeVersion}${safePagePath}`;
  }

  /**
   * Populates the version dropdown with available versions
   * @param {Object} manifest The version manifest
   * @param {string|null} currentVersion The current version
   */
  function populateVersionDropdown(manifest, currentVersion) {
    const select = document.getElementById('version-select');
    if (!select) {
      console.warn('Version select element not found');
      return;
    }

    // Clear existing options
    select.innerHTML = '';

    // Sort versions by version number (descending)
    const versions = manifest.versions.sort((a, b) => {
      const aNum = parseFloat(a.version.replace('v', ''));
      const bNum = parseFloat(b.version.replace('v', ''));
      return bNum - aNum;
    });

    // Add options for each version
    versions.forEach(versionInfo => {
      const option = document.createElement('option');
      option.value = versionInfo.version;
      option.textContent = versionInfo.version;

      if (versionInfo.isLatest) {
        option.textContent += ' (latest)';
      }

      if (versionInfo.version === currentVersion) {
        option.selected = true;
      }

      select.appendChild(option);
    });

    // Add change event listener
    select.addEventListener('change', function() {
      const selectedVersion = this.value;
      const pagePath = getCurrentPagePath(currentVersion);
      const newUrl = buildVersionUrl(selectedVersion, pagePath);
      window.location.href = newUrl;
    });
  }

  /**
   * Initializes the version switcher
   */
  async function initVersionSwitcher() {
    const manifest = await fetchVersionManifest();
    if (!manifest) {
      console.warn('Could not load version manifest, version switcher disabled');
      return;
    }

    const currentVersion = getCurrentVersion();
    populateVersionDropdown(manifest, currentVersion);
  }

  // Initialize when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initVersionSwitcher);
  } else {
    initVersionSwitcher();
  }
})();
