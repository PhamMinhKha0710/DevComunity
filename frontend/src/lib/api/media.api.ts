import apiClient from './client';

export const mediaApi = {
    upload: (formData: FormData) =>
        apiClient.post('/media/upload', formData, {
            headers: { 'Content-Type': undefined },
        }).then(r => r.data),
};
