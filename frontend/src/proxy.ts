import { NextRequest, NextResponse } from 'next/server';

const publicRoutes = ['/auth'];

const publicPrefixes = ['/api', '/_next', '/favicon', '/images', '/logo'];

export function proxy(request: NextRequest) {
    const { pathname } = request.nextUrl;

    if (publicPrefixes.some(p => pathname.startsWith(p)) || pathname.includes('.')) {
        return NextResponse.next();
    }

    const token = request.cookies.get('accessToken')?.value;

    const isPublicRoute = publicRoutes.some(r => pathname === r || pathname.startsWith(r + '/'));

    if (!token && pathname === '/') {
        return NextResponse.next();
    }

    if (!token && !isPublicRoute) {
        const loginUrl = new URL('/auth', request.url);
        loginUrl.searchParams.set('redirect', pathname);
        return NextResponse.redirect(loginUrl);
    }

    if (token && isPublicRoute) {
        return NextResponse.redirect(new URL('/', request.url));
    }

    return NextResponse.next();
}

export const config = {
    matcher: ['/((?!_next/static|_next/image|favicon.ico|images/).*)'],
};
