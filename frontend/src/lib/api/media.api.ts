import apiClient from './client';

export const mediaApi = {
    upload: (formData: FormData) =>
        apiClient.post('/media/upload', formData).then(r => r.data),
};
