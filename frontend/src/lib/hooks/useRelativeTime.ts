'use client';

import { useEffect, useState } from 'react';
// @ts-ignore - dayjs types may not be present
import dayjs from 'dayjs';
// @ts-ignore - dayjs plugin types may not be present
import relativeTime from 'dayjs/plugin/relativeTime';
// @ts-ignore - locale types not required
import 'dayjs/locale/vi';

dayjs.extend(relativeTime);
dayjs.locale('vi');

export function useRelativeTime(date: string | Date | null | undefined, refreshMs = 60000): string {
    const [, setNow] = useState<number>(() => Date.now());

    useEffect(() => {
        const id = setInterval(() => {
            setNow(Date.now());
        }, refreshMs);
        return () => clearInterval(id);
    }, [refreshMs]);

    if (!date) return '';

    const target = dayjs(date);
    if (!target.isValid()) return '';

    return target.from(dayjs());
}

