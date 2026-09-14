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
   * 3. Interactive 3D Book Showcase Engine
   * Features:
   * - Drag / Swipe to rotate 3D book in real-time (X & Y axis)
   * - Inertia & Momentum Damping physics via requestAnimationFrame
   * - Click to flip open / close front cover (165deg hinge)
   * - Distinguishes between click (open) and drag (rotate)
   * - Full mobile touch screen support
   */
  function initInteractive3DBook() {
    const stage = document.getElementById('hero3dStage');
    const rig = document.getElementById('book3dRig');
    const model = document.getElementById('book3dModel');
    const shadow = document.getElementById('book3dShadow');
    const btnToggle = document.getElementById('btnToggleOpenBook');
    const btnToggleText = document.getElementById('btnToggleOpenText');
    const btnReset = document.getElementById('btnResetBookView');
    const btnInsideClose = document.getElementById('btnInsideClose');

    if (!stage || !rig || !model) return;

    // Physics & Rotation State
    let currentRotX = 8;
    let currentRotY = -22;
    let targetRotX = 8;
    let targetRotY = -22;
    let velX = 0;
    let velY = 0;

    let isDragging = false;
    let startPointerX = 0;
    let startPointerY = 0;
    let lastPointerX = 0;
    let lastPointerY = 0;
    let dragDistance = 0;
    let isBookOpen = false;

    // Toggle Book Open / Close
    function toggleBook(forceState) {
      if (typeof forceState === 'boolean') {
        isBookOpen = forceState;
      } else {
        isBookOpen = !isBookOpen;
      }

      if (isBookOpen) {
        model.classList.add('book-opened');
        if (btnToggleText) btnToggleText.textContent = 'Gập Đóng Sách';
        // Xoay nhẹ sách hướng về người đọc khi mở để đọc trang trong dễ nhất
        targetRotX = 4;
        targetRotY = -6;
      } else {
        model.classList.remove('book-opened');
        if (btnToggleText) btnToggleText.textContent = 'Lật Mở Sách';
        targetRotX = 8;
        targetRotY = -22;
      }
    }

    // Reset to editorial standard angle
    function resetView() {
      targetRotX = isBookOpen ? 4 : 8;
      targetRotY = isBookOpen ? -6 : -22;
      velX = 0;
      velY = 0;
    }

    // Button event listeners
    if (btnToggle) {
      btnToggle.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        toggleBook();
      });
    }

    if (btnReset) {
      btnReset.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        resetView();
      });
    }

    if (btnInsideClose) {
      btnInsideClose.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        toggleBook(false);
      });
    }

    // Pointer Down (Mouse & Touch)
    function onPointerDown(clientX, clientY) {
      isDragging = true;
      startPointerX = clientX;
      startPointerY = clientY;
      lastPointerX = clientX;
      lastPointerY = clientY;
      dragDistance = 0;
      velX = 0;
      velY = 0;
    }

    // Pointer Move (Mouse & Touch)
    function onPointerMove(clientX, clientY) {
      if (!isDragging) return;

      const deltaX = clientX - lastPointerX;
      const deltaY = clientY - lastPointerY;

      dragDistance += Math.abs(deltaX) + Math.abs(deltaY);

      // Cập nhật góc xoay mục tiêu
      targetRotY += deltaX * 0.45;
      targetRotX -= deltaY * 0.35;

      // Giới hạn góc nghiêng trục X để sách luôn nhìn thuận mắt (-35deg đến 40deg)
      targetRotX = Math.max(-35, Math.min(40, targetRotX));
      // Giới hạn góc xoay trục Y (-85deg đến 65deg) để không lộn ngược gáy
      targetRotY = Math.max(-85, Math.min(65, targetRotY));

      velX = deltaX * 0.3;
      velY = deltaY * 0.3;

      lastPointerX = clientX;
      lastPointerY = clientY;
    }

    // Pointer Up (Mouse & Touch)
    function onPointerUp() {
      if (!isDragging) return;
      isDragging = false;

      // Nếu kéo ít hơn 6px, coi như là click chuột -> mở hoặc đóng sách
      if (dragDistance < 7) {
        toggleBook();
      }
    }

    // Mouse Events on Stage
    stage.addEventListener('mousedown', (e) => {
      // Bỏ qua nếu click vào nút bấm bên trong trang sách
      if (e.target.closest('a') || e.target.closest('button')) return;
      onPointerDown(e.clientX, e.clientY);
    });

    window.addEventListener('mousemove', (e) => {
      if (isDragging) {
        onPointerMove(e.clientX, e.clientY);
      }
    });

    window.addEventListener('mouseup', () => {
      if (isDragging) {
        onPointerUp();
      }
    });

    // Touch Events on Stage (Mobile Support)
    stage.addEventListener('touchstart', (e) => {
      if (e.target.closest('a') || e.target.closest('button')) return;
      if (e.touches.length === 1) {
        onPointerDown(e.touches[0].clientX, e.touches[0].clientY);
      }
    }, { passive: true });

    window.addEventListener('touchmove', (e) => {
      if (isDragging && e.touches.length === 1) {
        onPointerMove(e.touches[0].clientX, e.touches[0].clientY);
      }
    }, { passive: true });

    window.addEventListener('touchend', () => {
      if (isDragging) {
        onPointerUp();
      }
    });

    // Subtle Idle Hover Tilt (khi di chuột qua stage mà không bấm giữ)
    stage.addEventListener('mousemove', (e) => {
      if (isDragging || prefersReducedMotion) return;
      const rect = stage.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width - 0.5;
      const y = (e.clientY - rect.top) / rect.height - 0.5;

      // Nghiêng nhẹ theo trỏ chuột
      targetRotY += x * 0.15;
      targetRotX -= y * 0.15;
    });

    // Render Animation Loop (60fps Physics & Damping)
    let animationFrameId = null;
    let idleTime = 0;

    function animateBook() {
      if (!prefersReducedMotion) {
        // Quán tính khi thả tay
        if (!isDragging) {
          targetRotY += velX;
          targetRotX -= velY;
          velX *= 0.88;
          velY *= 0.88;

          // Hiệu ứng nhịp thở nhẹ nhàng khi đứng yên
          idleTime += 0.02;
          const subtleFloatY = Math.sin(idleTime) * 0.08;
          const subtleFloatX = Math.cos(idleTime * 0.8) * 0.05;
          targetRotY += subtleFloatY;
          targetRotX += subtleFloatX;
        }

        // Interpolation mượt mà (Easing Lerp)
        currentRotX += (targetRotX - currentRotX) * 0.12;
        currentRotY += (targetRotY - currentRotY) * 0.12;

        rig.style.transform = `rotateX(${currentRotX.toFixed(2)}deg) rotateY(${currentRotY.toFixed(2)}deg)`;

        // Đồng bộ bóng đổ chân sách theo góc xoay
        if (shadow) {
          const shadowOffsetX = -(currentRotY * 0.8).toFixed(1);
          const shadowSkew = (currentRotY * 0.2).toFixed(1);
          shadow.style.transform = `translateX(calc(-50% + ${shadowOffsetX}px)) rotateX(85deg) skewX(${shadowSkew}deg) translateZ(-60px)`;
        }
      }

      animationFrameId = requestAnimationFrame(animateBook);
    }

    animateBook();
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
    initInteractive3DBook();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

