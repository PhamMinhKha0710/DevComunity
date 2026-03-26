import { NextRequest, NextResponse } from 'next/server';

/** Đăng nhập / đăng ký — user đã có token sẽ bị chuyển về trang chủ */
const authEntryRoutes = ['/auth'];

/** Quên mật khẩu / đặt lại mật khẩu — luôn cho phép, kể cả khi đã đăng nhập */
const passwordRecoveryRoutes = ['/forgot-password', '/reset-password'];

const publicPrefixes = ['/api', '/_next', '/favicon', '/images', '/logo'];

function matchesRoute(pathname: string, routes: string[]) {
    return routes.some((r) => pathname === r || pathname.startsWith(`${r}/`));
}

export function proxy(request: NextRequest) {
    const { pathname } = request.nextUrl;

    if (publicPrefixes.some((p) => pathname.startsWith(p)) || pathname.includes('.')) {
        return NextResponse.next();
    }

    const token = request.cookies.get('accessToken')?.value;

    const isAuthEntry = matchesRoute(pathname, authEntryRoutes);
    const isPasswordRecovery = matchesRoute(pathname, passwordRecoveryRoutes);
    const isPublicRoute = isAuthEntry || isPasswordRecovery;

    if (!token && pathname === '/') {
        return NextResponse.next();
    }

    if (!token && !isPublicRoute) {
        const loginUrl = new URL('/auth', request.url);
        loginUrl.searchParams.set('redirect', pathname);
        return NextResponse.redirect(loginUrl);
    }

    if (token && isAuthEntry) {
        return NextResponse.redirect(new URL('/', request.url));
    }

    return NextResponse.next();
}

export const config = {
    matcher: ['/((?!_next/static|_next/image|favicon.ico|images/).*)'],
};
