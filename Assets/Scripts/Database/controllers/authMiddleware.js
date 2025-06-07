const jwt = require('jsonwebtoken');
const User = require('../models/User');

module.exports = async (req, res, next) => {
    const token = req.header('Authorization')?.replace('Bearer ', '');
    if (!token) {
        return res.status(401).send('Acesso negado. Token não fornecido.');
    }

    try {
        const decoded = jwt.verify(token, process.env.JWT_SECRET);

        const user = await User.findById(decoded.userId);

        if (!user) {
            return res.status(401).send('Acesso negado. Utilizador não encontrado.');
        }

        req.user = user;
        next();
    } catch (err) {
        res.status(400).send('Token inválido');
    }
};