import apiClient from './client';

export const mediaApi = {
    upload: (formData: FormData) =>
        apiClient.post('/media/upload', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        }).then(r => r.data),
};
