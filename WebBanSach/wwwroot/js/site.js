/**
 * PageCraft Booksellers - Editorial Scroll Experience
 * Features:
 * 1. Scroll Progress Bar (3px Terracotta #C85A32)
 * 2. Dynamic Glassmorphism Navbar (compact & blur on scroll > 50px)
 * 3. Hero Parallax & 3D Interactive Floating Book
 * 4. Scroll Reveal & Staggered Animations (IntersectionObserver, 60fps)
 * 5. Dynamic Counter Numbers on Scroll
 * 6. Full Accessibility (prefers-reduced-motion)
 */

(function () {
  'use strict';

  // Check user preference for reduced motion
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // DOM Elements cache
  let progressBar = null;
  let mainNavbar = null;
  let heroSection = null;
  let ambient1 = null;
  let ambient2 = null;
  let ambient3 = null;
  let hero3DBook = null;

  // Animation frame request ID for throttling
  let ticking = false;

  /**
   * 1. Initialize Scroll Progress Bar
   */
  function updateScrollProgressBar() {
    if (!progressBar) return;

    const scrollTop = window.scrollY || document.documentElement.scrollTop;
    const docHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    const scrollPercentage = docHeight > 0 ? Math.min(100, Math.max(0, (scrollTop / docHeight) * 100)) : 0;

    progressBar.style.width = scrollPercentage.toFixed(2) + '%';
    progressBar.setAttribute('aria-valuenow', Math.round(scrollPercentage));
  }

  /**
   * 2. Initialize Glassmorphism Navbar Transformation
   */
  function updateNavbarOnScroll() {
    if (!mainNavbar) return;

    const scrollY = window.scrollY || document.documentElement.scrollTop;
    if (scrollY > 50) {
      if (!mainNavbar.classList.contains('navbar-scrolled')) {
        mainNavbar.classList.add('navbar-scrolled');
      }
    } else {
      if (mainNavbar.classList.contains('navbar-scrolled')) {
        mainNavbar.classList.remove('navbar-scrolled');
      }
    }
  }

  /**
   * 3. Hero Parallax & 3D Tilt Effect
   */
  function updateHeroParallax() {
    if (prefersReducedMotion || !heroSection) return;

    const scrollY = window.scrollY || document.documentElement.scrollTop;
    const heroHeight = heroSection.offsetHeight || 600;

    // Only update parallax if hero is within view
    if (scrollY < heroHeight + 100) {
      if (ambient1) {
        ambient1.style.transform = `translate3d(0, ${(scrollY * 0.28).toFixed(1)}px, 0)`;
      }
      if (ambient2) {
        ambient2.style.transform = `translate3d(0, ${(-scrollY * 0.18).toFixed(1)}px, 0)`;
      }
      if (ambient3) {
        ambient3.style.transform = `translate3d(0, ${(scrollY * 0.12).toFixed(1)}px, 0)`;
      }

      if (hero3DBook) {
        const tiltX = (3 - scrollY * 0.03).toFixed(2);
        const tiltY = (-5 + scrollY * 0.04).toFixed(2);
        const translateY = (-scrollY * 0.18).toFixed(1);
        hero3DBook.style.transform = `translate3d(0, ${translateY}px, 0) rotateX(${tiltX}deg) rotateY(${tiltY}deg)`;
      }
    }
  }

  /**
   * Unified On-Scroll Handler (Throttled via requestAnimationFrame)
   */
  function onScroll() {
    if (!ticking) {
      window.requestAnimationFrame(() => {
        updateScrollProgressBar();
        updateNavbarOnScroll();
        updateHeroParallax();
        ticking = false;
      });
      ticking = true;
    }
  }

  /**
   * 4. Scroll Reveal & Staggered Animations using IntersectionObserver
   */
  function initScrollReveal() {
    const revealTargets = document.querySelectorAll('.reveal-on-scroll, .reveal-stagger');

    if (prefersReducedMotion) {
      revealTargets.forEach(el => el.classList.add('is-revealed'));
      return;
    }

    if (!('IntersectionObserver' in window)) {
      revealTargets.forEach(el => el.classList.add('is-revealed'));
      return;
    }

    const observerOptions = {
      root: null,
      rootMargin: '0px 0px -50px 0px',
      threshold: 0.1
    };

    const revealObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-revealed');
          observer.unobserve(entry.target);
        }
      });
    }, observerOptions);

    revealTargets.forEach(target => {
      revealObserver.observe(target);
    });
  }

  /**
   * 5. Dynamic Counter Numbers on Scroll
   */
  function initCounterAnimation() {
    const statsSection = document.getElementById('editorialStats');
    const counterElements = document.querySelectorAll('.stat-counter[data-counter-target]');

    if (!counterElements.length) return;

    if (prefersReducedMotion) {
      counterElements.forEach(el => {
        const target = parseInt(el.getAttribute('data-counter-target'), 10) || 0;
        el.textContent = target.toLocaleString('vi-VN');
      });
      return;
    }

    // Easing function: easeOutQuart
    function easeOutQuart(x) {
      return 1 - Math.pow(1 - x, 4);
    }

    function animateCounter(el) {
      const target = parseInt(el.getAttribute('data-counter-target'), 10) || 0;
      const duration = 2000; // ms
      const startTime = performance.now();

      function updateNumber(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const currentVal = Math.round(target * easeOutQuart(progress));

        el.textContent = currentVal.toLocaleString('vi-VN');

        if (progress < 1) {
          requestAnimationFrame(updateNumber);
        } else {
          el.textContent = target.toLocaleString('vi-VN');
        }
      }

      requestAnimationFrame(updateNumber);
    }

    if (!('IntersectionObserver' in window)) {
      counterElements.forEach(el => animateCounter(el));
      return;
    }

    const counterObserver = new IntersectionObserver((entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          counterElements.forEach(el => animateCounter(el));
          observer.unobserve(entry.target);
        }
      });
    }, {
      root: null,
      threshold: 0.2
    });

    if (statsSection) {
      counterObserver.observe(statsSection);
    } else {
      counterElements.forEach(el => counterObserver.observe(el));
    }
  }

  /**
   * Hero 3D Book Interactive Hover Tilt
   */
  function initHero3DHover() {
    if (prefersReducedMotion) return;
    const stage = document.querySelector('.hero-book-stage');
    if (!stage || !hero3DBook) return;

    stage.addEventListener('mousemove', (e) => {
      const rect = stage.getBoundingClientRect();
      const x = e.clientX - rect.left - rect.width / 2;
      const y = e.clientY - rect.top - rect.height / 2;

      const tiltX = -(y / (rect.height / 2)) * 8;
      const tiltY = (x / (rect.width / 2)) * 10;

      hero3DBook.style.transform = `perspective(1200px) rotateX(${tiltX.toFixed(2)}deg) rotateY(${tiltY.toFixed(2)}deg) scale3d(1.02, 1.02, 1.02)`;
    });

    stage.addEventListener('mouseleave', () => {
      hero3DBook.style.transform = '';
      updateHeroParallax();
    });
  }

  /**
   * DOM Ready Initialization
   */
  function init() {
    progressBar = document.getElementById('scrollProgressBar');
    mainNavbar = document.getElementById('mainNavbar');
    heroSection = document.getElementById('heroSection');
    ambient1 = document.getElementById('ambientCircle1');
    ambient2 = document.getElementById('ambientCircle2');
    ambient3 = document.getElementById('ambientCircle3');
    hero3DBook = document.getElementById('hero3DBook');

    // Register scroll event listener
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll, { passive: true });

    // Initial triggers
    updateScrollProgressBar();
    updateNavbarOnScroll();
    updateHeroParallax();

    // Init animations
    initScrollReveal();
    initCounterAnimation();
    initHero3DHover();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

