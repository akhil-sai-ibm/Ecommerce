import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';

const SignalRContext = createContext({ connection: null });

export const useSignalR = () => useContext(SignalRContext);

export const SignalRProvider = ({ children }) => {
    const { isAuthenticated, user } = useAuth();
    const [connection, setConnection] = useState(null);

    useEffect(() => {
        let newConnection = null;
        const token = localStorage.getItem('token');

        if (isAuthenticated && user && token) {
            const baseUrl = import.meta.env.VITE_API_BASE_URL || 'https://localhost:52549/api';
            const hubUrl = baseUrl.replace(/\/api\/?$/, '/hubs/orders');

            newConnection = new signalR.HubConnectionBuilder()
                .withUrl(hubUrl, {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect()
                .build();

            newConnection.start()
                .then(() => {
                    setConnection(newConnection);
                })
                .catch(e => {
                    console.error('SignalR Connection Error: ', e);
                });
        }

        return () => {
            if (newConnection) {
                newConnection.stop();
            }
            setConnection(null);
        };
    }, [isAuthenticated, user]);

    return (
        <SignalRContext.Provider value={{ connection }}>
            {children}
        </SignalRContext.Provider>
    );
};
